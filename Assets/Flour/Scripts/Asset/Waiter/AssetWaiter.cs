﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Flour.Asset
{
	public class AssetWaiter<T> where T : UnityEngine.Object
	{
		public string Key { get; private set; }

		readonly List<Request<T>> requests = new List<Request<T>>();

		WaiterBridge bridge;

		public AssetWaiter(string key) => Key = key.ToLower();

		internal void SetBridge(WaiterBridge bridge)
		{
			this.bridge = bridge;

			this.bridge.OnAssetLoaded += OnLoaded;
			this.bridge.OnDownloadedError += OnError;
			this.bridge.OnLoadedError += OnError;

			this.bridge.AddWaiter(Key, ContainsRequest, FindRequests, Dispose);
		}

		internal virtual void Dispose()
		{
			this.bridge.OnAssetLoaded -= OnLoaded;
			this.bridge.OnDownloadedError -= OnError;
			this.bridge.OnLoadedError -= OnError;
		}

		public long GetSize(string assetBundleName)
		{
#if UNITY_EDITOR && USE_LOCAL_ASSET
			return 0;
#else
			var ab = string.Intern(Path.Combine(Key, assetBundleName));
			return bridge.SizeManiefst.GetSize(ab);
#endif
		}

		protected virtual T GetAsset(UnityEngine.Object asset)
		{
			return asset != null ? (T)asset : null;
		}

		public IObservable<T> LoadAsync(string assetName, string valiant = "")
		{
			return LoadAsync(assetName, assetName, valiant);
		}

		public virtual IObservable<T> LoadAsync(string assetBundleName, string assetName, string valiant = "")
		{
			assetBundleName = assetBundleName.ToLower();

			if (!string.IsNullOrEmpty(valiant))
			{
				assetBundleName += $".{valiant}";
			}

			var ab = string.Intern(Path.Combine(Key, assetBundleName));

#if UNITY_EDITOR && USE_LOCAL_ASSET
			var localAsset = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(ab, assetName);
			if (localAsset.Length > 0)
			{
				return Observable.Return<T>((T)UnityEditor.AssetDatabase.LoadAssetAtPath(localAsset[0], typeof(T)));
			}
#endif

			var req = FindOrDefault(assetBundleName, assetName);
			if (req != null)
			{
				return req.Subject;
			}
			req = new Request<T>(ab, bridge.Manifest.GetAllDependencies(ab), assetName);
			requests.Add(req);

			bridge.AddRequest(req);
			return req.Subject;
		}

		Request<T> FindOrDefault(string assetBundleName, string assetName)
		{
			for (int i = 0; i < requests.Count; i++)
			{
				if (requests[i].Equals(assetBundleName, assetName)) return requests[i];
			}
			return null;
		}

		bool ContainsRequest(string assetBundleName)
		{
			for (int i = 0; i < requests.Count; i++)
			{
				if (requests[i].Containts(assetBundleName)) return true;
			}
			return false;
		}
		IEnumerable<IAssetRequest> FindRequests(string assetBundleName)
		{
			for (int i = 0; i < requests.Count; i++)
			{
				if (string.IsNullOrEmpty(assetBundleName) || requests[i].Containts(assetBundleName)) yield return requests[i];
			}
		}

		void OnLoaded(string assetBundleName, string assetName, UnityEngine.Object asset)
		{
			if (!assetBundleName.StartsWith(Key, StringComparison.Ordinal)) return;

			for (int i = requests.Count - 1; i >= 0; i--)
			{
				var req = requests[i];
				if (!req.Equals(assetBundleName, assetName)) continue;

				if (req.Subject.HasObservers)
				{
					req.Subject.OnNext(asset == null ? null : GetAsset(asset));
					req.Subject.OnCompleted();
				}
				else
				{
					req.Subject.Dispose();
				}

				requests.Remove(req);
				bridge.CleanRequest(req.AssetBundleNames);
			}
		}

		void OnError(string assetBundleName) => OnLoaded(assetBundleName, "", null);
		void OnError(string assetBundleName, string assetName) => OnLoaded(assetBundleName, assetName, null);
	}
}
