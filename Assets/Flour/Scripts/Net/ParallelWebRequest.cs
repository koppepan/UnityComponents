﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Flour.Net
{
	public interface IDownloader<T>
	{
		string FilePath { get; }

		bool IsDone { get; }
		bool IsError { get; }

		long ResponseCode { get; }
		string Error { get; }
		float Progress { get; }

		void Send(string baseUrl, int timeout);
		void Update();
		T GetContent();
		void Dispose();
	}

	public abstract class ParallelWebRequest<T>
	{
		readonly WaitForSeconds waitForSeconds = new WaitForSeconds(0.1f);

		string baseUrl;

		readonly int parallel;
		readonly int timeout;

		readonly List<IDownloader<T>> waitingList = new List<IDownloader<T>>();
		readonly List<IDownloader<T>> downloaders = new List<IDownloader<T>>();

		readonly Subject<Tuple<string, T>> downloadedObserver = new Subject<Tuple<string, T>>();
		readonly Subject<Tuple<string, long, string>> erroredObserver = new Subject<Tuple<string, long, string>>();

		public IObservable<Tuple<string, T>> DownloadedObservable { get { return downloadedObserver; } }
		public IObservable<Tuple<string, long, string>> ErroredObservable { get { return erroredObserver; } }

		int downloadedCount = 0;

		CompositeDisposable updateDisposable;

		private readonly FloatReactiveProperty downloadedCountProperty = new FloatReactiveProperty(0);
		public IReactiveProperty<float> DownloadedCount { get { return downloadedCountProperty; } }

		public ParallelWebRequest(string baseUrl, int parallel, int timeout)
		{
			this.baseUrl = baseUrl;

			this.parallel = parallel;
			this.timeout = timeout;
		}

		public void ChangeBaseUrl(string baseUrl) => this.baseUrl = baseUrl;

		public void Dispose()
		{
			StopUpdate();

			waitingList.ForEach(x => x.Dispose());
			waitingList.Clear();
			downloaders.ForEach(x => x.Dispose());
			downloaders.Clear();

			downloadedObserver.OnCompleted();
			downloadedObserver.Dispose();

			erroredObserver.OnCompleted();
			erroredObserver.Dispose();
		}

		internal void ResetProgressCount()
		{
			downloadedCount = 0;
		}
		void StartUpdate()
		{
			if (updateDisposable != null)
			{
				return;
			}

			updateDisposable = new CompositeDisposable();
			Observable.FromCoroutine(EveryUpdate).Subscribe().AddTo(updateDisposable);
			Observable.EveryLateUpdate().Subscribe(UpdateProgress).AddTo(updateDisposable);
		}
		void StopUpdate()
		{
			if (updateDisposable != null)
			{
				updateDisposable.Dispose();
				updateDisposable = null;
			}
		}

		public void AddRequest(IDownloader<T> downloader)
		{
			if (waitingList.Any(x => x.FilePath.Equals(downloader.FilePath, StringComparison.Ordinal))) return;
			if (downloaders.Any(x => x.FilePath.Equals(downloader.FilePath, StringComparison.Ordinal))) return;

			waitingList.Add(downloader);

			StartUpdate();
		}

		IEnumerator EveryUpdate()
		{
			while (true)
			{
				if (downloaders.Count == 0 && waitingList.Count == 0)
				{
					StopUpdate();
				}

				for (int i = downloaders.Count - 1; i >= 0; i--)
				{
					var d = downloaders[i];
					d.Update();
					if (d.IsDone || d.IsError)
					{
						downloaders.Remove(d);

						if (d.IsError)
						{
							erroredObserver.OnNext(Tuple.Create(d.FilePath, d.ResponseCode, d.Error));
						}
						else
						{
							downloadedObserver.OnNext(Tuple.Create(d.FilePath, d.GetContent()));

							downloadedCount++;
							UpdateProgress(0);
						}
					}
				}

				while (waitingList.Count > 0 && downloaders.Count < parallel)
				{
					var req = waitingList[0];
					req.Send(baseUrl, timeout);

					waitingList.Remove(req);
					downloaders.Add(req);
				}

				yield return waitForSeconds;
			}
		}

		void UpdateProgress(long _)
		{
			float currentProgress = 0;
			for (int i = 0; i < downloaders.Count; i++)
			{
				if (downloaders[i].IsDone || downloaders[i].IsError) continue;
				currentProgress += downloaders[i].Progress;
			}

			downloadedCountProperty.Value = downloadedCount + currentProgress;
		}
	}
}
