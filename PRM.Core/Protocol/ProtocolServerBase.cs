﻿using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ColorPickerWPF.Code;
using Newtonsoft.Json;
using Shawn.Utils;
using Color = System.Windows.Media.Color;

namespace PRM.Core.Protocol
{
    public abstract class ProtocolServerBase : NotifyPropertyChangedBase, ICloneable
    {
        protected ProtocolServerBase(string protocol, string classVersion, string protocolDisplayName, bool onlyOneInstance = true)
        {
            Protocol = protocol;
            ClassVersion = classVersion;
            ProtocolDisplayName = protocolDisplayName;
            OnlyOneInstance = onlyOneInstance;
        }

        private bool _onlyOneInstance = true;
        public bool OnlyOneInstance
        {
            get => _onlyOneInstance;
            private set => SetAndNotifyIfChanged(nameof(OnlyOneInstance), ref _onlyOneInstance, value);
        }


        private uint _id = 0;
        [JsonIgnore]
        public uint Id
        {
            get => _id;
            set => SetAndNotifyIfChanged(nameof(Id), ref _id, value);
        }

        public string Protocol { get; }

        public string ClassVersion { get; }

        public string ProtocolDisplayName { get; }


        private string _dispName = "";
        public string DispName
        {
            get => _dispName;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    SetAndNotifyIfChanged(nameof(DispName), ref _dispName, value);
                }
            }
        }

        [JsonIgnore]
        public string SubTitle => GetSubTitle();


        private string _groupName = "";
        public string GroupName
        {
            get => _groupName;
            set => SetAndNotifyIfChanged(nameof(GroupName), ref _groupName, value);
        }


        private string _iconBase64 = "";
        public string IconBase64
        {
            get => _iconBase64;
            set
            {
                SetAndNotifyIfChanged(nameof(IconBase64), ref _iconBase64, value);
                try
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        var bm = NetImageProcessHelper.BitmapFromBytes(Convert.FromBase64String(value));
                        _iconImg = bm.ToBitmapSource();
                        Icon = bm.ToIcon();
                    }
                }
                catch (Exception e)
                {
                    SimpleLogHelper.Debug(e, e.StackTrace);
                    _iconImg = null;
                    Icon = null;
                }
            }
        }


        private Icon _icon = null;
        [JsonIgnore]
        public Icon Icon
        {
            get => _icon;
            private set => SetAndNotifyIfChanged(nameof(Icon), ref _icon, value);
        }


        private BitmapSource _iconImg = null;
        [JsonIgnore]
        public BitmapSource IconImg
        {
            get => _iconImg;
            set
            {
                SetAndNotifyIfChanged(nameof(IconImg), ref _iconImg, value);
                try
                {
                    if (value != null)
                    {
                        _iconBase64 = Convert.ToBase64String(value.ToBytes());
                        Icon = value.ToIcon();
                    }
                }
                catch (Exception e)
                {
                    SimpleLogHelper.Error(e, e.StackTrace);
                    _iconBase64 = null;
                    Icon = null;
                }
            }
        }


        private string _markColorHex = "#FFFFFF";
        public string MarkColorHex
        {
            get => _markColorHex;
            set
            {
                try
                {
                    var color = (Color)System.Windows.Media.ColorConverter.ConvertFromString(value);
                    SetAndNotifyIfChanged(nameof(MarkColorHex), ref _markColorHex, value);
                    SetAndNotifyIfChanged(nameof(MarkColor), ref _markColor, color);
                }
                catch (Exception)
                {
                }
            }
        }


        private Color _markColor = Colors.White;
        [JsonIgnore]
        public Color MarkColor
        {
            get => _markColor;
            set
            {
                SetAndNotifyIfChanged(nameof(MarkColorHex), ref _markColorHex, value.ToHexString());
                SetAndNotifyIfChanged(nameof(MarkColor), ref _markColor, value);
            }
        }

        private DateTime _lastConnTime = DateTime.MinValue;
        public DateTime LastConnTime
        {
            get => _lastConnTime;
            set => SetAndNotifyIfChanged(nameof(LastConnTime), ref _lastConnTime, value);
        }

        /// <summary>
        /// copy all value type fields
        /// </summary>
        public bool Update(ProtocolServerBase copyFromObj, Type levelType = null)
        {
            var baseType = levelType;
            if (baseType == null)
                baseType = this.GetType();
            var myType = this.GetType();
            var yourType = copyFromObj.GetType();
            while (myType != null && myType != baseType)
            {
                myType = myType.BaseType;
            }
            while (yourType != null && yourType != baseType)
            {
                yourType = yourType.BaseType;
            }
            if (myType != null && myType == yourType)
            {
                ProtocolServerBase copyObject = this;
                while (yourType != null)
                {
                    var fields = myType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    foreach (var fi in fields)
                    {
                        if (!fi.IsInitOnly)
                            fi.SetValue(this, fi.GetValue(copyFromObj));
                    }
                    var properties = myType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    foreach (var property in properties)
                    {
                        if (property.CanWrite && property.SetMethod != null)
                        {
                            // update properties without notify
                            property.SetValue(this, property.GetValue(copyFromObj));
                            // then raise notify
                            base.RaisePropertyChanged(property.Name);
                        }
                    }
                    // update base class
                    yourType = yourType.BaseType;
                }
            }
            return false;
        }


        ///// <summary>
        ///// copy all value ProtocolServerBase fields
        ///// </summary>
        //public bool Update(ProtocolServerBase copyFromObj)
        //{
        //    var baseType = levelType;
        //    ProtocolServerBase copyObject = this;
        //    var fields = baseType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        //    foreach (var fi in fields)
        //    {
        //        fi.SetValue(this, fi.GetValue(copyFromObj));
        //    }
        //    var properties = baseType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        //    foreach (var property in properties)
        //    {
        //        if (property.SetMethod != null)
        //        {
        //            // update properties without notify
        //            property.SetValue(this, property.GetValue(copyFromObj));
        //            // then raise notify
        //            base.RaisePropertyChanged(property.Name);
        //        }
        //    }
        //    return false;
        //}
        public virtual string ToJsonString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// json string to instance
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public abstract ProtocolServerBase CreateFromJsonString(string jsonString);


        /// <summary>
        /// subtitle of every server, different form each protocol
        /// </summary>
        /// <returns></returns>
        protected abstract string GetSubTitle();

        /// <summary>
        /// determine the display order to show items
        /// </summary>
        /// <returns></returns>
        public abstract int GetListOrder();

        /// <summary>
        /// cation: it is a shallow
        /// </summary>
        public object Clone()
        {
            return MemberwiseClone();
        }

        public virtual bool EqualTo(ProtocolServerBase compare)
        {
            var t1 = this.GetType();
            var t2 = compare.GetType();
            if (t1 == t2)
            {
                var properties = t1.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var property in properties)
                {
                    if (property.CanWrite && property.SetMethod != null)
                    {
                        var v1 = property.GetValue(this)?.ToString();
                        var v2 = property.GetValue(compare)?.ToString();
                        if (v1 != v2)
                            return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
