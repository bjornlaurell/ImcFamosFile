﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImcFamosFile
{
    public class FamosFileComponent : FamosFileBaseExtended
    {
        #region Constructors

        public FamosFileComponent()
        {
            //
        }

        public FamosFileComponent(BinaryReader reader,
                                  int codePage,
                                  FamosFileXAxisScaling? currentXAxisScaling,
                                  FamosFileZAxisScaling? currentZAxisScaling,
                                  FamosFileTriggerTimeInfo? currentTriggerTimeInfo) : base(reader, codePage)
        {
            this.XAxisScaling = currentXAxisScaling;
            this.ZAxisScaling = currentZAxisScaling;
            this.TriggerTimeInfo = currentTriggerTimeInfo;

            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                // index
                var index = this.DeserializeInt32();

                if (index != 1 && index != 2)
                    throw new FormatException($"Expected index value '1' or '2', got {index}");

                this.Index = index;

                // analog / digital
                var analogDigital = this.DeserializeInt32();

                if (analogDigital != 1 && analogDigital != 2)
                    throw new FormatException($"Expected analog / digital value '1' or '2', got {analogDigital}");

                this.IsDigital = analogDigital == 2;
            });

            while (true)
            {
                var nextKeyType = this.DeserializeKeyType();

                // end of CC reached
                if (nextKeyType == FamosFileKeyType.CT ||
                    nextKeyType == FamosFileKeyType.CB ||
                    nextKeyType == FamosFileKeyType.CI ||
                    nextKeyType == FamosFileKeyType.CG)
                {
                    // go back to start of key
                    this.Reader.BaseStream.Position -= 4;
                    break;
                }

                else if (nextKeyType == FamosFileKeyType.Unknown)
                {
                    this.SkipKey();
                    continue;
                }

                else if (nextKeyType == FamosFileKeyType.CD)
                    this.XAxisScaling = new FamosFileXAxisScaling(this.Reader, this.CodePage);

                else if (nextKeyType == FamosFileKeyType.CZ)
                    this.ZAxisScaling = new FamosFileZAxisScaling(this.Reader, this.CodePage);

                else if (nextKeyType == FamosFileKeyType.NT)
                    this.TriggerTimeInfo = new FamosFileTriggerTimeInfo(this.Reader);

                else if (nextKeyType == FamosFileKeyType.CP)
                    this.PackInfo = new FamosFilePackInfo(this.Reader);

                else if (nextKeyType == FamosFileKeyType.Cb)
                {
                    this.DeserializeKey(expectedKeyVersion: 1, keySize =>
                    {
                        var bufferCount = this.DeserializeInt32();
                        var userInfoSize = this.DeserializeInt32();

                        for (int i = 0; i < bufferCount; i++)
                        {
                            var buffer = new FamosFileBuffer(this.Reader);
                            this.Buffers.Add(buffer);
                        }
                    });
                }

                else if (nextKeyType == FamosFileKeyType.CR)
                    this.CalibrationInfo = new FamosFileCalibrationInfo(this.Reader, this.CodePage);

                else if (nextKeyType == FamosFileKeyType.ND)
                    this.DisplayInfo = new FamosFileDisplayInfo(this.Reader);

                else if (nextKeyType == FamosFileKeyType.Cv)
                    this.EventInfo = new FamosFileEventInfo(this.Reader);

                else if (nextKeyType == FamosFileKeyType.CN)
                    this.ChannelInfos.Add(new FamosFileChannelInfo(this.Reader, this.CodePage));

                else
                    // should never happen
                    throw new FormatException("An unexpected state has been reached.");
            }
        }

        #endregion

        #region Properties

        public int Index { get; set; }
        public bool IsDigital { get; set; }

        public FamosFileXAxisScaling? XAxisScaling { get; set; }
        public FamosFileZAxisScaling? ZAxisScaling { get; set; }
        public FamosFileTriggerTimeInfo? TriggerTimeInfo { get; set; }

        public FamosFilePackInfo? PackInfo { get; set; }
        public FamosFileCalibrationInfo? CalibrationInfo { get; set; }
        public FamosFileDisplayInfo? DisplayInfo { get; set; }
        public FamosFileEventInfo? EventInfo { get; set; }

        public List<FamosFileBuffer> Buffers { get; private set; } = new List<FamosFileBuffer>();
        public List<FamosFileChannelInfo> ChannelInfos { get; } = new List<FamosFileChannelInfo>();

        #endregion

        #region Relay Properties

        public string Name
        {
            get
            {
                var name = string.Empty;

                foreach (var channelInfo in this.ChannelInfos)
                {
                    if (!string.IsNullOrWhiteSpace(channelInfo.Name))
                    {
                        name = channelInfo.Name;
                        break;
                    }
                }

                return name;
            }
        }

        #endregion

        #region Methods

        internal override void Prepare()
        {
            // sort buffers
            this.Buffers = this.Buffers.OrderBy(buffer => buffer.Reference).ToList();

            // associate buffers to pack info
            this.PackInfo?.Buffers.AddRange(this.Buffers.Where(buffer => buffer.Reference == this.PackInfo.BufferReference));
        }

        internal override void Validate()
        {
            // analog vs. digital
            if (!this.IsDigital && this.CalibrationInfo is null)
                throw new FormatException($"The analog component '{this.Name}' does not define calibration information.");

            if (this.IsDigital && this.CalibrationInfo != null)
                throw new FormatException($"The digital component '{this.Name}' defines analog calibration information.");

            // pack info
            if (this.PackInfo is null)
                throw new FormatException("The component's pack info must be provided.");

            if (!this.PackInfo.Buffers.Any())
                throw new FormatException("The pack info's buffers collection must container at least a single buffer instance.");

            foreach (var buffer in this.PackInfo.Buffers)
            {
                if (!this.Buffers.Contains(buffer))
                    throw new FormatException("The pack info's buffers must be part of the component's buffer collection.");
            }

            // validate buffers
            foreach (var buffer in this.Buffers)
            {
                buffer.Validate();
            }

            // validate display info
            this.DisplayInfo?.Validate();

            // validate pack info
            this.PackInfo?.Validate();
        }

        #endregion
    }
}