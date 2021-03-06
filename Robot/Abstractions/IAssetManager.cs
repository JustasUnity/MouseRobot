﻿using System;
using System.Collections.Generic;

namespace Robot.Abstractions
{
    public interface IAssetManager
    {
        IEnumerable<Asset> Assets { get; }
        Dictionary<Guid, Asset> GuidAssetTable { get; }
        Dictionary<Guid, long> GuidHashTable { get; }
        Dictionary<Guid, string> GuidPathTable { get; }

        bool IsEditingAssets { get; }

        event Action<string> AssetCreated;
        event Action<string> AssetDeleted;
        event Action<string, string> AssetRenamed;
        event Action<string> AssetUpdated;
        event Action RefreshFinished;

        void BeginAssetEditing();

        void SaveExistngAsset(Asset existingAsset, object newValue);
        Asset CreateAsset(object assetValue, string path);
        void DeleteAsset(string path);
        void EditAssets(Action ac);
        void EndAssetEditing();
        Asset GetAsset(string path);
        Asset GetAsset(string folder, string name);
        void Refresh();
        void RenameAsset(string sourcePath, string destPath);
    }
}