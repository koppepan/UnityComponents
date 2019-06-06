﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UniRx.Async;

namespace Example
{
	public class FadeLayer : AbstractSubLayer
	{
		Image panel;

		protected override void OnOpen()
		{
			base.OnOpen();
			panel = GetComponent<Image>();
		}

		private IEnumerator Fade(float time, Color to)
		{
			bool complete = false;
			LeanTween.color(panel.rectTransform, to, time).setOnComplete(() => complete = true);

			while (!complete) yield return null;
		}

		public async UniTask FadeIn(float time = 0.2f)
		{
			panel.color = Color.clear;
			await Fade(time, Color.black);
		}
		public async UniTask FadeOut(float time = 0.2f)
		{
			panel.color = Color.black;
			await Fade(time, Color.clear);
			await CloseWait();
		}
	}
}
