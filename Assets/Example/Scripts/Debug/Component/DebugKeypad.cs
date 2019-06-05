﻿using UnityEngine;
using UnityEngine.UI;

namespace Example
{
	public class DebugKeypad : MonoBehaviour
	{
		[SerializeField]
		private Text inputText;
		[SerializeField]
		private Button runButton;
		[SerializeField]
		private Transform keyParent;

		string input;

		public void SetupFloat(System.Action<double> onRun)
		{
			Initialize();
			RegisterEvent(true, () => onRun?.Invoke(double.Parse(input)));
		}
		public void SetupInteger(System.Action<long> onRun)
		{
			Initialize();
			RegisterEvent(false, () => onRun?.Invoke(long.Parse(input)));
		}

		private void RegisterEvent(bool isFloat, System.Action action)
		{
			runButton.onClick.AddListener(() =>
			{
				action?.Invoke();
				Initialize();
			});

			for (int i = 0; i < 10; i++)
			{
				int num = i;
				keyParent.Find($"Button{i}").GetComponent<Button>().onClick.AddListener(() => Add(num.ToString()));
			}
			keyParent.Find("Button<").GetComponent<Button>().onClick.AddListener(() => Remove());

			var dotButton = keyParent.Find("Button.").GetComponent<Button>();
			dotButton.onClick.AddListener(() => Add("."));
			dotButton.interactable = isFloat;
		}

		private void Initialize()
		{
			input = "";
			inputText.text = "0";
		}
		private void UpdateText(string text)
		{
			inputText.text = text;
		}
		private void Add(string text)
		{
			input += text;
			UpdateText(input);
		}
		private void Remove()
		{
			if (string.IsNullOrEmpty(input))
			{
				return;
			}
			input = input.Remove(input.Length - 1, 1);
			UpdateText(string.IsNullOrEmpty(input) ? "0" : input);
		}
	}
}
