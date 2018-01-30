﻿using Robot.Abstractions;
using RobotRuntime.Abstractions;
using RobotRuntime.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Robot
{
    public class AssetManager : IAssetManager
    {
        public Dictionary<Guid, Asset> GuidAssetTable { get; private set; } = new Dictionary<Guid, Asset>();
        public Dictionary<Guid, string> GuidPathTable { get; private set; } = new Dictionary<Guid, string>();
        public Dictionary<Guid, Int64> GuidHashTable { get; private set; } = new Dictionary<Guid, Int64>();

        public event Action RefreshFinished;
        public event Action<string, string> AssetRenamed;
        public event Action<string> AssetDeleted;
        public event Action<string> AssetCreated;
        public event Action<string> AssetUpdated;

        private bool m_ShouldSaveMetadata = true;

        private IAssetGuidManager AssetGuidManager;
        private IProfiler Profiler;
        public AssetManager(IAssetGuidManager AssetGuidManager, IProfiler Profiler)
        {
            this.AssetGuidManager = AssetGuidManager;
            this.Profiler = Profiler;
        }

        public void Refresh()
        {
            Profiler.Start("AssetManager_Refresh");
            BeginAssetEditing();

            var paths = Paths.GetAllFilePaths();
            var assetsOnDisk = paths.Select(path => new Asset(path));

            // Detect renamed assets if application was closed, and assets were renamed via file system
            foreach (var pair in AssetGuidManager.Paths.ToList())
            {
                var path = pair.Value;
                if (!File.Exists(path))
                {
                    var guid = pair.Key;
                    var hash = AssetGuidManager.GetHash(guid);

                    var assetOnDiskWithSameHashButNotKnownPath = assetsOnDisk.FirstOrDefault(
                        a => a.Hash == hash && !AssetGuidManager.ContainsValue(a.Path));

                    // If this asset on disk is found, update old guid to new path, since best prediction is that it was renamed
                    if (assetOnDiskWithSameHashButNotKnownPath != null)
                        AssetGuidManager.AddNewGuid(guid, assetOnDiskWithSameHashButNotKnownPath.Path, hash);
                }
            }

            // Detect Rename for assets in memory (while keeping existing asset references)
            foreach (var assetInMemory in Assets.ToList())
            {
                if (!File.Exists(assetInMemory.Path))
                {
                    // if known path does not exist on disk anymore but some other asset with same hash exists on disk, it must have been renamed
                    var assetWithSameHashAndNotInDbYet = assetsOnDisk.FirstOrDefault(asset =>
                    asset.Hash == assetInMemory.Hash && !GuidPathTable.ContainsValue(asset.Path));

                    if (assetWithSameHashAndNotInDbYet != null)
                        RenameAssetInternal(assetInMemory.Path, assetWithSameHashAndNotInDbYet.Path);
                    else
                        DeleteAssetInternal(assetInMemory);
                }
            }

            // Add new assets and detect modifications
            foreach (var assetOnDisk in assetsOnDisk)
            {
                var isHashKnown = GuidHashTable.ContainsValue(assetOnDisk.Hash);
                var isPathKnown = GuidPathTable.ContainsValue(assetOnDisk.Path);

                // We know the path, but hash has changed, must have been modified
                if (!isHashKnown && isPathKnown)
                {
                    GetAsset(assetOnDisk.Path).UpdateValueFromDisk();
                    AssetUpdated?.Invoke(assetOnDisk.Path);
                }
                // New file added
                else if (!isPathKnown)
                {
                    AddAssetInternal(assetOnDisk);
                }
            }

            EndAssetEditing();
            Profiler.Stop("AssetManager_Refresh");

            RefreshFinished?.Invoke();
        }

        public Asset CreateAsset(object assetValue, string path)
        {
            path = Paths.GetProjectRelativePath(path);
            var asset = GetAsset(path);
            if (asset != null)
            {
                asset.Importer.Value = assetValue;
                asset.Importer.SaveAsset();
                asset.Update();
                AssetUpdated?.Invoke(path);
            }
            else
            {
                asset = new Asset(path);
                asset.Importer.Value = assetValue;
                asset.Importer.SaveAsset();
                asset.Update();
                AddAssetInternal(asset);
            }

            AssetGuidManager.AddNewGuid(asset.Guid, asset.Path, asset.Hash);
            if (m_ShouldSaveMetadata)
                AssetGuidManager.Save();

            return asset;
        }

        /// <summary>
        /// Removes asset from memory and deletes its corresponding file from disk
        /// </summary>
        public void DeleteAsset(string path)
        {
            path = Paths.GetProjectRelativePath(path);
            var asset = GetAsset(path);

            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);

            DeleteAssetInternal(asset);
        }

        private void DeleteAssetInternal(Asset asset, bool silent = false)
        {
            GuidAssetTable.Remove(asset.Guid);
            GuidPathTable.Remove(asset.Guid);
            GuidHashTable.Remove(asset.Guid);

            if (!silent)
                AssetDeleted?.Invoke(asset.Path);
        }

        /// <summary>
        /// Renames asset from memory and renames its corresponding file. Asset will keep the same guid and GuidMap will be updated
        /// </summary>
        public void RenameAsset(string sourcePath, string destPath)
        {
            var asset = GetAsset(sourcePath);

            File.SetAttributes(sourcePath, FileAttributes.Normal);
            File.Move(sourcePath, destPath);

            RenameAssetInternal(sourcePath, destPath);
        }

        private void RenameAssetInternal(string sourcePath, string destPath)
        {
            var asset = GetAsset(sourcePath);
            var value = asset.Importer.Value;
            var guid = asset.Guid;

            DeleteAssetInternal(asset, true);
            asset.UpdatePath(destPath);

            AssetGuidManager.AddNewGuid(guid, asset.Path, asset.Hash);
            if (m_ShouldSaveMetadata)
                AssetGuidManager.Save();

            AddAssetInternal(asset, true);

            AssetRenamed?.Invoke(sourcePath, destPath);
        }

        public Asset GetAsset(string path)
        {
            path = Paths.GetProjectRelativePath(path);
            return Assets.FirstOrDefault((a) => Paths.AreRelativePathsEqual(a.Path, path));
        }

        public Asset GetAsset(string folder, string name)
        {
            var path = folder + "\\" + name + "." + Paths.GetExtensionFromFolder(folder);
            return GetAsset(path);
        }

        public void BeginAssetEditing()
        {
            m_ShouldSaveMetadata = false;
        }

        public void EndAssetEditing()
        {
            m_ShouldSaveMetadata = true;
            AssetGuidManager.Save();
        }

        public void EditAssets(Action ac)
        {
            BeginAssetEditing();
            ac.Invoke();
            EndAssetEditing();
        }

        public IEnumerable<Asset> Assets
        {
            get
            {
                return GuidAssetTable.Select(pair => pair.Value);
            }
        }

        private void AddAssetInternal(Asset asset, bool silent = false)
        {
            var guid = AssetGuidManager.GetGuid(asset.Path);
            if (guid != default(Guid))
                asset.SetGuid(guid);

            GuidAssetTable.Add(asset.Guid, asset);
            GuidPathTable.Add(asset.Guid, asset.Path);
            GuidHashTable.Add(asset.Guid, asset.Hash);

            AssetGuidManager.AddNewGuid(asset.Guid, asset.Path, asset.Hash);
            if (m_ShouldSaveMetadata)
                AssetGuidManager.Save();

            if (!silent)
                AssetCreated?.Invoke(asset.Path);
        }
    }
}
