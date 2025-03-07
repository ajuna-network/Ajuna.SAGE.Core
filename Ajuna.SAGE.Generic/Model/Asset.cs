﻿using Ajuna.SAGE.Model;
using System;
using System.Buffers;
using System.Linq;

namespace Ajuna.SAGE.Core.Model
{
    /// <summary>
    /// Asset class
    /// </summary>
    public class Asset : IAsset
    {
        public uint Id { get; set; }

        public uint OwnerId { get; set; }

        public byte CollectionId { get; }

        public uint Score { get; set; }

        public uint Genesis { get; set; }

        public byte[]? Data { get; set; }

        public bool IsLockable { get; set; }

        public virtual byte MatchTypeSize { get; set; } = 1;

        /// <summary>
        /// Asset constructor
        /// </summary>
        /// <param name="id"></param>
        /// <param name="collectionId"></param>
        /// <param name="score"></param>
        /// <param name="genesis"></param>
        /// <param name="data"></param>
        public Asset(uint id, uint ownerId, byte collectionId, uint score, uint genesis, byte[]? data)
        {
            Id = id;
            OwnerId = ownerId;
            CollectionId = collectionId;
            Score = score;
            Genesis = genesis;
            Data = data;
        }

        /// <summary>
        /// Empty asset
        /// </summary>
        /// <param name="id"></param>
        /// <param name="collectionId"></param>
        /// <returns></returns>
        public static Asset Empty(uint id, uint ownerId, byte collectionId)
        {
            Asset avatar = new Asset(id, ownerId, collectionId, 0, 0, null);
            return avatar;
        }

        /// <inheritdoc/>
        public bool Equals(IAsset? other)
        {
            if (other == null)
            {
                return false;
            }

            return Id == other.Id;
        }

        /// <inheritdoc/>
        public virtual byte[] MatchType => Data != null && Data.Length > 3 ? Data.Take(MatchTypeSize).ToArray() : Array.Empty<byte>();

        /// <inheritdoc/>
        public bool OwnedBy(IAccount account)
        {
            return OwnerId == account.Id;
        }

        /// <summary>
        /// Map to domain
        /// </summary>
        /// <param name="dbAsset"></param>
        /// <returns></returns>
        public static Asset MapToDomain(DbAsset dbAsset) =>
            new Asset(dbAsset.Id, dbAsset.OwnerId, dbAsset.CollectionId, dbAsset.Score, dbAsset.Genesis, dbAsset.Data);
    }
}