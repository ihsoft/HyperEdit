﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyperEdit.View
{
    public interface IView
    {
        void Draw();
    }

    public class CustomView : IView
    {
        private readonly Action draw;

        public CustomView(Action draw)
        {
            this.draw = draw;
        }

        public void Draw()
        {
            draw();
        }
    }

    public class ConditionalView : IView
    {
        private readonly Func<bool> doDisplay;
        private readonly IView toDisplay;

        public ConditionalView(Func<bool> doDisplay, IView toDisplay)
        {
            this.doDisplay = doDisplay;
            this.toDisplay = toDisplay;
        }

        public void Draw()
        {
            if (doDisplay())
                toDisplay.Draw();
        }
    }

    public class LabelView : IView
    {
        private readonly GUIContent label;

        public LabelView(string label, string help)
        {
            this.label = new GUIContent(label, help);
        }

        public void Draw()
        {
            GUILayout.Label(label);
        }
    }

    public class VerticalView : IView
    {
        private readonly ICollection<IView> views;

        public VerticalView(ICollection<IView> views)
        {
            this.views = views;
        }

        public void Draw()
        {
            GUILayout.BeginVertical();
            foreach (var view in views)
            {
                view.Draw();
            }
            GUILayout.EndVertical();
        }
    }

    public class ButtonView : IView
    {
        private readonly GUIContent label;
        private readonly Action onChange;

        public ButtonView(string label, string help, Action onChange)
        {
            this.label = new GUIContent(label, help);
            this.onChange = onChange;
        }

        public void Draw()
        {
            if (GUILayout.Button(label))
            {
                onChange();
            }
        }
    }

    public class ToggleView : IView
    {
        private readonly GUIContent label;
        private readonly Func<bool> getValue;
        private readonly Func<bool> isValid;
        private readonly Action<bool> onChange;

        public ToggleView(string label, string help, Func<bool> getValue, Func<bool> isValid, Action<bool> onChange)
        {
            this.label = new GUIContent(label, help);
            this.getValue = getValue;
            this.isValid = isValid;
            this.onChange = onChange;
        }

        public void Draw()
        {
            var oldValue = getValue();
            var newValue = GUILayout.Toggle(oldValue, label);
            if (oldValue != newValue && isValid())
                onChange(newValue);
        }
    }

    public class SliderView : IView
    {
        private readonly Action<double> onChange;
        private readonly GUIContent label;

        public double Value { get; set; }

        public SliderView(string label, string help, Action<double> onChange = null)
        {
            this.onChange = onChange;
            this.label = new GUIContent(label, help);
            Value = 0;
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            var newValue = (double)GUILayout.HorizontalSlider((float)Value, 0, 1);
            if (Math.Abs(newValue - Value) > 0.01)
            {
                Value = newValue;
                if (onChange != null)
                    onChange(Value);
            }
            GUILayout.EndHorizontal();
        }
    }

    public class ListSelectView<T> : IView
    {
        private readonly Func<IEnumerable<T>> list;
        private readonly Func<T, string> toString;
        private readonly Action<T> onSelect;
        private T currentlySelected;

        public T CurrentlySelected
        {
            get { return currentlySelected; }
            set
            {
                currentlySelected = value;
                if (onSelect != null)
                    onSelect(value);
            }
        }

        public ListSelectView(Func<IEnumerable<T>> list, Action<T> onSelect = null, Func<T, string> toString = null)
        {
            this.list = list;
            this.toString = toString ?? (x => x.ToString());
            this.onSelect = onSelect;
            this.currentlySelected = default(T);
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(currentlySelected == null ? "<none>" : toString(currentlySelected));
            if (GUILayout.Button("Select"))
            {
                var realList = list();
                if (realList != null)
                    WindowHelper.Selector("Select", realList, toString, t => CurrentlySelected = t);
            }
            GUILayout.EndHorizontal();
        }
    }

    public class TextBoxView<T> : IView
    {
        private readonly GUIContent label;
        private readonly View.TryParse<T> parser;
        private readonly Func<T, string> toString;
        private readonly Action<T> onSet;
        private string value;
        private T obj;

        public bool Valid { get; private set; }

        public T Object
        {
            get { return obj; }
            set
            {
                this.value = toString(value);
                obj = value;
            }
        }

        public TextBoxView(string label, string help, T start, View.TryParse<T> parser, Func<T, string> toString = null, Action<T> onSet = null)
        {
            this.label = label == null ? null : new GUIContent(label, help);
            this.toString = toString ?? (x => x.ToString());
            value = this.toString(start);
            this.parser = parser;
            this.onSet = onSet;
        }

        public void Draw()
        {
            if (label != null || onSet != null)
            {
                GUILayout.BeginHorizontal();
                if (label != null)
                    GUILayout.Label(label);
            }

            T tempValue;
            Valid = parser(value, out tempValue);

            if (Valid)
            {
                value = GUILayout.TextField(value);
                obj = tempValue;
            }
            else
            {
                var color = GUI.color;
                GUI.color = Color.red;
                value = GUILayout.TextField(value);
                GUI.color = color;
            }
            if (label != null || onSet != null)
            {
                if (onSet != null && Valid && GUILayout.Button("Set"))
                    onSet(Object);
                GUILayout.EndHorizontal();
            }
        }
    }

    public class TabView : IView
    {
        private readonly List<KeyValuePair<string, IView>> views;
        private KeyValuePair<string, IView> current;

        public TabView(List<KeyValuePair<string, IView>> views)
        {
            this.views = views;
            this.current = views[0];
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            foreach (var view in views)
            {
                if (view.Key == current.Key)
                {
                    GUILayout.Button(view.Key, Extentions.PressedButton);
                }
                else
                {
                    if (GUILayout.Button(view.Key))
                        current = view;
                }
            }
            GUILayout.EndHorizontal();
            current.Value.Draw();
        }
    }

    public abstract class View
    {
        private Dictionary<string, string> _textboxInputs = new Dictionary<string, string>();

        public delegate bool TryParse<T>(string str,out T value);

        private bool _allValid = true;

        protected bool AllValid { get { return _allValid; } }

        protected T GuiTextField<T>(string key, GUIContent display, TryParse<T> parser, T value, Func<T, string> toString = null)
        {
            if (display != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(display);
            }
            if (_textboxInputs.ContainsKey(key) == false)
                _textboxInputs[key] = toString == null ? value.ToString() : toString(value);

            T tempValue;
            var isValid = parser(_textboxInputs[key], out tempValue);
            if (!isValid)
                _allValid = false;

            if (isValid)
            {
                _textboxInputs[key] = GUILayout.TextField(_textboxInputs[key]);
            }
            else
            {
                var color = GUI.color;
                GUI.color = Color.red;
                _textboxInputs[key] = GUILayout.TextField(_textboxInputs[key]);
                GUI.color = color;
            }
            if (display != null)
            {
                GUILayout.EndHorizontal();
            }
            return isValid ? tempValue : value;
        }

        protected T? GuiTextFieldSettable<T>(string key, GUIContent display, TryParse<T> parser, T value, Func<T, string> toString = null) where T : struct
        {
            GUILayout.BeginHorizontal();
            if (display != null)
                GUILayout.Label(display);
            value = GuiTextField(key, null, parser, value, toString);
            var set = GUILayout.Button("Set", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            return set ? value : (T?)null;
        }

        protected float Slider(GUIContent display, float oldval, Model.SliderRange range, ref bool changed)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(display);
            var newval = GUILayout.HorizontalSlider(oldval, range.Min, range.Max);
            GUILayout.EndHorizontal();
            if (changed == false)
                changed = newval != oldval;
            return newval;
        }

        protected void ClearTextFields()
        {
            _textboxInputs.Clear();
        }

        public virtual void Draw(Window window)
        {
            _allValid = true;
        }

        public static void CreateView(object model)
        {
            var planet = model as Model.PlanetEditor;
            var sma = model as Model.SmaAligner;
            if (planet != null)
                PlanetEditorView.Create(planet);
            if (sma != null)
                SmaAlignerView.Create(sma);
        }
    }
}
