﻿using System;
using System.Collections.Generic;
using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// A group is a collection of texts, single values and channels.
    /// </summary>
    public class FamosFileGroup : FamosFileBaseProperty
    {
        #region Fields

        private int _index;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileGroup"/> class.
        /// </summary>
        /// <param name="name">The name of this group.</param>
        public FamosFileGroup(string name)
        {
            this.Name = name;
        }

        internal FamosFileGroup(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.Index = this.DeserializeInt32();
                this.Name = this.DeserializeString();
                this.Comment = this.DeserializeString();
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of this group.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the comment of this group.
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Gets a list of texts.
        /// </summary>
        public List<FamosFileText> Texts { get; private set; } = new List<FamosFileText>();

        /// <summary>
        /// Gets a list of single values.
        /// </summary>
        public List<FamosFileSingleValue> SingleValues { get; private set; } = new List<FamosFileSingleValue>();

        /// <summary>
        /// Gets a list of channels.
        /// </summary>
        public List<FamosFileChannel> Channels { get; private set; } = new List<FamosFileChannel>();

        internal int Index
        {
            get { return _index; }
            set
            {
                if (value <= 0)
                    throw new FormatException($"Expected index > '0', got '{value}'.");

                _index = value;
            }
        }

        protected override FamosFileKeyType KeyType => FamosFileKeyType.CB;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                this.Index,
                this.Name.Length, this.Name,
                this.Comment.Length, this.Comment
            };

            this.SerializeKey(writer, 1, data);
            base.Serialize(writer);
        }

        #endregion
    }
}
