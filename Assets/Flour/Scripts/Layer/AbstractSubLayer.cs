﻿using System;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Flour.Layer
{
	[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup))]
	public abstract class AbstractSubLayer<TLayerKey, TSubKey> : MonoBehaviour where TLayerKey : struct where TSubKey : struct
	{
		public TSubKey Key { get; private set; }
		private TLayerKey layerKey;

		private Action<TLayerKey, AbstractSubLayer<TLayerKey, TSubKey>> moveFront;
		private Action<TLayerKey, RectTransform> safeAreaExpansion;
		private Func<AbstractSubLayer<TLayerKey, TSubKey>, bool, UniTask> onDestroy;

		public virtual bool IgnoreBack { get { return false; } }

		private CanvasGroup _canvasGroup;
		protected CanvasGroup CanvasGroup
		{
			get
			{
				if (_canvasGroup != null) return _canvasGroup;
				_canvasGroup = GetComponent<CanvasGroup>();
				return _canvasGroup;
			}
		}

		private GraphicRaycaster _raycaster;
		private GraphicRaycaster Raycaster
		{
			get
			{
				if (_raycaster != null) return _raycaster;
				_raycaster = GetComponent<GraphicRaycaster>();
				return _raycaster;
			}
		}

		public bool Interactable
		{
			get
			{
				return Raycaster.enabled;
			}
			set
			{
				Raycaster.enabled = value;
			}
		}

		public float Alpha
		{
			get
			{
				return CanvasGroup.alpha;
			}
			set
			{
				CanvasGroup.alpha = value;
			}
		}

		internal void SetConstParameter(TLayerKey layerKey, TSubKey key,
			Action<TLayerKey, AbstractSubLayer<TLayerKey, TSubKey>> moveFront,
			Action<TLayerKey, RectTransform> safeAreaExpansion,
			Func<AbstractSubLayer<TLayerKey, TSubKey>, bool, UniTask> onDestroy
			)
		{
			Key = key;
			this.layerKey = layerKey;

			this.moveFront = moveFront;
			this.safeAreaExpansion = safeAreaExpansion;
			this.onDestroy = onDestroy;
		}

		public void MoveFront() => moveFront(layerKey, this);
		public void Close(bool force = false) => onDestroy(this, force);
		public async UniTask CloseWait(bool force = false) => await onDestroy(this, force);

		internal void OnOpenInternal() => OnOpen();
		internal async UniTask OnCloseInternal(bool force) => await OnClose(force);
		internal void OnBackInternal() => OnBack();
		internal void OnChangeSiblingIndexInternal(int index) => OnChangeSiblingIndex(index);

		protected virtual void OnOpen() { }
		protected virtual async UniTask OnClose(bool force)
		{
			Destroy(gameObject);
			await UniTask.DelayFrame(1);
		}
		protected virtual void OnBack() => Close();
		protected virtual void OnChangeSiblingIndex(int index) { }

		protected void SafeAreaExpansion() => SafeAreaExpansion(GetComponent<RectTransform>());
		protected void SafeAreaExpansion(RectTransform rect) => safeAreaExpansion(layerKey, rect);
	}
}
