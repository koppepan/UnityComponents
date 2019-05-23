﻿using System;
using UnityEngine;

namespace Flour.Layer
{
	public abstract class AbstractSubLayer : MonoBehaviour
	{
		public LayerType Layer { get; private set; }
		public SubLayerType SubLayer { get; private set; }
		public virtual bool IgnoreBack { get { return false; } }

		Action<LayerType, RectTransform> safeAreaExpansion;
		Action<AbstractSubLayer> onDestroy;

		internal void SetConstParameter(LayerType layer, SubLayerType subLayer, Action<LayerType, RectTransform> safeAreaExpansion, Action<AbstractSubLayer> onDestroy)
		{
			Layer = layer;
			SubLayer = subLayer;

			this.safeAreaExpansion = safeAreaExpansion;
			this.onDestroy = onDestroy;
		}

		public virtual void Close() => onDestroy?.Invoke(this);

		public virtual void OnOpen() { }
		public virtual void OnClose() => Destroy(gameObject);

		public virtual void OnBack() { }
		public virtual void OnChangeSiblingIndex(int index) { }

		protected void SafeAreaExpansion() => safeAreaExpansion?.Invoke(Layer, GetComponent<RectTransform>());
		protected void SafeAreaExpansion(RectTransform rect) => safeAreaExpansion?.Invoke(Layer, rect);
	}
}
