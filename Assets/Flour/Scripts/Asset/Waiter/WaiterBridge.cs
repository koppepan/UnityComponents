﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Flour.Asset
{
	internal class WaiterBridge
	{
		internal delegate void AddRequestDelegate(IAssetRequest request);
		internal delegate void CleanRequestDelegate(string assetBundleName);

		internal delegate bool ContainsRequestDelegate(string assetBundleName);
		internal delegate IEnumerable<IAssetRequest> FindRequestsDelegate(string assetBundleName);
		internal delegate void WaiterDispose();

		public AssetBundleManifest Manifest { get; private set; }
		public AssetBundleSizeManifest SizeManiefst { get; private set; }

		private AddRequestDelegate addRequestDelegate;
		private CleanRequestDelegate cleanRequestDelegate;

		private List<Tuple<string, ContainsRequestDelegate>> contains = new List<Tuple<string, ContainsRequestDelegate>>();
		private List<FindRequestsDelegate> requests = new List<FindRequestsDelegate>();
		private List<WaiterDispose> waiterDisposes = new List<WaiterDispose>();

		public event Action<string, string, UnityEngine.Object> OnAssetLoaded = delegate { };
		public event Action<string> OnDownloadedError = delegate { };
		public event Action<string, string> OnLoadedError = delegate { };

		public WaiterBridge(AddRequestDelegate addRequest, CleanRequestDelegate cleanRequest)
		{
			addRequestDelegate = addRequest;
			cleanRequestDelegate = cleanRequest;
		}

		public void SetManifest(AssetBundleManifest manifest, AssetBundleSizeManifest sizeManifest)
		{
			Manifest = manifest;
			SizeManiefst = sizeManifest;
		}

		public void AddWaiter(string key, ContainsRequestDelegate containsRequest, FindRequestsDelegate getRequests, WaiterDispose dispose)
		{
			contains.Add(Tuple.Create(key, containsRequest));
			requests.Add(getRequests);
			waiterDisposes.Add(dispose);
		}

		public void Dispose()
		{
			waiterDisposes.ForEach(x => x());
			SetManifest(null, null);
		}

		public void AddRequest(IAssetRequest request) => addRequestDelegate(request);
		public void CleanRequest(string[] assetBundleNames)
		{
			for (int i = 0; i < assetBundleNames.Length; i++)
			{
				if (!ContainsRequest(assetBundleNames[i])) cleanRequestDelegate(assetBundleNames[i]);
			}
		}

		bool ContainsRequest(string assetBundleName)
		{
			for (int i = 0; i < contains.Count; i++)
			{
				if (assetBundleName.StartsWith(contains[i].Item1, StringComparison.Ordinal) && contains[i].Item2(assetBundleName)) return true;
			}
			return false;
		}

		public IEnumerable<IAssetRequest> FindRequests(string assetBundleName = "")
		{
			return requests.SelectMany(x => x.Invoke(assetBundleName));
		}

		public void OnLoaded(string assetBundleName, string assetName, UnityEngine.Object asset) => OnAssetLoaded.Invoke(assetBundleName, assetName, asset);
		public void OnError(string assetBundleName) => OnDownloadedError.Invoke(assetBundleName);
		public void OnError(string assetBundleName, string assetName) => OnLoadedError.Invoke(assetBundleName, assetName);
	}
}
