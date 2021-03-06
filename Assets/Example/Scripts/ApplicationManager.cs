﻿using Flour;
using System;
using System.Linq;
using UniRx;
using UniRx.Async;
using UnityEngine;

namespace Example
{
	using LayerHandler = Flour.Layer.LayerHandler<LayerType, SubLayerType>;
	using SceneHandler = Flour.Scene.SceneHandler<Tuple<IOperationBundler, AssetHandler>>;

	public sealed class ApplicationManager : MonoBehaviour
	{
		[Header("UI")]
		[SerializeField]
		Transform canvasRoot = default;
		[SerializeField]
		Vector2 referenceResolution = new Vector2(750, 1334);
		[SerializeField]
		LayerType[] safeAreaLayers = new LayerType[] { };

		// 初期化時にPrefabをLoadしておくSubLayer一覧
		readonly SubLayerType[] FixedSubLayers = new SubLayerType[] { SubLayerType.Blackout, SubLayerType.Footer };

		private ApplicationOperator appOperator;

		private void Awake()
		{
			DontDestroyObjectList.Add<ApplicationManager>(gameObject);
			DontDestroyObjectList.Add<LayerHandler>(canvasRoot.gameObject);
		}

		async void Start()
		{
			Observable.FromEvent(
				_ => Application.lowMemory += OnLowMemory,
				_ => Application.lowMemory -= OnLowMemory).Subscribe().AddTo(this);

			var sceneHandler = new SceneHandler();
			var layerHandler = new LayerHandler();

			foreach (var t in EnumExtension.ToEnumerable<LayerType>(x => LayerType.Scene != x && LayerType.Debug != x))
			{
				var safeArea = safeAreaLayers.Contains(t);
				layerHandler.AddLayer(t, t.ToOrder(), canvasRoot, referenceResolution, safeArea);
			}


			var repository = new SubLayerSourceRepository();
			repository.AddRepository(FixedSubLayers, FixedSubLayers.Length);
			repository.AddRepository(EnumExtension.ToEnumerable<SubLayerType>(x => !FixedSubLayers.Contains(x)), 10);

			await repository.PreLoadAsync(FixedSubLayers);


#if !USE_LOCAL_ASSET && USE_SECURE_ASSET
			var pass = await AssetHelper.GetPasswordAsync();
			var assetHandler = new AssetHandler("", AssetHelper.CacheAssetPath, pass);
#else
			var assetHandler = new AssetHandler("");
#endif

			appOperator = new ApplicationOperator(ApplicationQuit, assetHandler, sceneHandler, layerHandler, repository);
			await appOperator.LoadSceneAsync(SceneType.Start);

			InitializeDebug(sceneHandler, layerHandler, repository);

			// AndroidのBackKey対応
			Observable.EveryUpdate()
				.Where(_ => Input.GetKeyDown(KeyCode.Escape))
				.ThrottleFirst(TimeSpan.FromMilliseconds(500))
				.Subscribe(_ => appOperator.OnBack()).AddTo(this);

			// EditorをPauseしたときにOnApplicationPauseと同じ挙動にする
#if UNITY_EDITOR
			Observable.FromEvent<UnityEditor.PauseState>(
				h => UnityEditor.EditorApplication.pauseStateChanged += h,
				h => UnityEditor.EditorApplication.pauseStateChanged -= h).Subscribe(PauseStateChanged).AddTo(this);
#endif
		}

		private void OnLowMemory()
		{
			Debug.LogWarning("low memory");
			appOperator.ResourceCompress().Forget();
		}

#if UNITY_EDITOR
		private void PauseStateChanged(UnityEditor.PauseState state)
		{
			var pause = state == UnityEditor.PauseState.Paused;
#else
		private void OnApplicationPause(bool pause)
		{
#endif
			appOperator?.ApplicationPause(pause);
		}

		private void ApplicationQuit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.ExitPlaymode();
#else
			Application.Quit(0);
#endif
		}
		private void OnApplicationQuit()
		{
			appOperator.Dispose();
			DontDestroyObjectList.Clear();
		}

		private void InitializeDebug(SceneHandler sceneHandler, LayerHandler layerHandler, SubLayerSourceRepository repository)
		{
#if DEBUG_BUILD
			layerHandler.AddLayer(LayerType.Debug, LayerType.Debug.ToOrder(), canvasRoot, referenceResolution, false);

			var debugHandler = new GameObject("DebugHandler", typeof(DebugHandler)).GetComponent<DebugHandler>();

			repository.AddDebugRepository();
			debugHandler.Initialize(new DebugDialogCreator(sceneHandler, layerHandler, repository));

			DontDestroyObjectList.Add<DebugHandler>(debugHandler.gameObject);
#endif
		}
	}
}

