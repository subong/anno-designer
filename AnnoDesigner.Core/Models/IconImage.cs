﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace AnnoDesigner.Core.Models
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class IconImage
    {
        private BitmapImage _icon;

        #region ctor

        public IconImage(string name)
        {
            Name = name;
            Localizations = null;
        }

        public IconImage(string name, Dictionary<string, string> localizations, string iconPath) : this(name)
        {
            Localizations = localizations;
            IconPath = iconPath;
        }

        #endregion        

        public string Name { get; }

        public Dictionary<string, string> Localizations { get; set; }

        public string DisplayName
        {
            get { return NameForLanguage("eng"); }
        }

        public string NameForLanguage(string language)
        {
            if (Localizations is null || !Localizations.TryGetValue(language, out var translation))
            {
                return Name;
            }

            return translation;
        }

        public BitmapImage Icon
        {
            get
            {
                if (_icon == null && !string.IsNullOrWhiteSpace(IconPath))
                {
                    _icon = new BitmapImage(new Uri(IconPath));
                    if (_icon.CanFreeze)
                    {
                        _icon.Freeze();
                    }
                }

                return _icon;
            }
        }

        public string IconPath { get; }
    }
}