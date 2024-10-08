﻿using System;
using System.Threading.Tasks;
using Onova;
using Onova.Models;
using Onova.Services;

namespace Mapping_Tools.Updater {

    public interface IUpdateManager {
        Progress<double> Progress { get; }
        IPackageResolver PackageResolver { get; }
        public CheckForUpdatesResult UpdatesResult { get; }
        bool RestartAfterUpdate { get; set; }

        Task<bool> FetchUpdateAsync();

        /// <exception cref="InvalidOperationException"></exception>
        Task DownloadUpdateAsync();

        /// <exception cref="Onova.Exceptions.LockFileNotAcquiredException"></exception>
        /// <exception cref="Onova.Exceptions.UpdaterAlreadyLaunchedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        void StartUpdateProcess();
    }

    public class UpdateManager : IUpdateManager, IDisposable {
        private bool hasDownloaded;
        private Onova.IUpdateManager updateManager;

        public Progress<double> Progress { get; private set; }
        public IPackageResolver PackageResolver { get; private set; }
        public CheckForUpdatesResult UpdatesResult { get; private set; }
        public bool RestartAfterUpdate { get; set; }

        public UpdateManager(IPackageResolver packageResolver) {
            PackageResolver = packageResolver;

            Setup();
        }

        public UpdateManager(string repoOwner, string repoName, string assetNamePattern) {
            PackageResolver = new GithubPackageResolver(repoOwner, repoName, assetNamePattern);

            Setup();
        }

        private void Setup() {
            updateManager = new Onova.UpdateManager(PackageResolver, new ZipPackageExtractor());
            Progress = new Progress<double>();
        }

        public async Task<bool> FetchUpdateAsync() {
            UpdatesResult = await updateManager.CheckForUpdatesAsync();

            return UpdatesResult.CanUpdate;
        }

        /// <exception cref="InvalidOperationException"></exception>
        public async Task DownloadUpdateAsync() {
            if (UpdatesResult?.LastVersion == null) {
                throw new InvalidOperationException("请勿在获取更新之前调用此方法！");
            }

            if (!UpdatesResult.CanUpdate) {
                throw new InvalidOperationException("请勿在没有可用更新时调用此方法！");
            }

            await updateManager.PrepareUpdateAsync(UpdatesResult.LastVersion, Progress);

            hasDownloaded = true;
        }

        /// <exception cref="Onova.Exceptions.LockFileNotAcquiredException"></exception>
        /// <exception cref="Onova.Exceptions.UpdaterAlreadyLaunchedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void StartUpdateProcess() {
            if (UpdatesResult?.LastVersion == null) {
                throw new InvalidOperationException("请勿在获取更新之前调用此方法！");
            }

            if (!hasDownloaded) {
                throw new InvalidOperationException("请勿在下载完成前调用此方法！");
            }

            updateManager.LaunchUpdater(UpdatesResult.LastVersion, RestartAfterUpdate);
        }

        public void Dispose() {
            GC.SuppressFinalize(this);

            updateManager.Dispose();
        }
    }
}