﻿#pragma checksum "D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\src\MyDriving\src\MobileApps\MyDriving\MyDriving.UWP\Views\GetStarted5.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "9965F57D2E2B6BB90CF0068F8FA483C4"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MyDriving.UWP.Views
{
    partial class GetStarted5 : 
        global::Windows.UI.Xaml.Controls.Page, 
        global::Windows.UI.Xaml.Markup.IComponentConnector,
        global::Windows.UI.Xaml.Markup.IComponentConnector2
    {
        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 14.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                {
                    this.Getstarted5 = (global::Windows.UI.Xaml.Controls.Image)(target);
                }
                break;
            case 2:
                {
                    this.Dots = (global::MyDriving.UWP.Controls.DotsControl)(target);
                }
                break;
            case 3:
                {
                    this.DoneButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    #line 24 "..\..\..\Views\GetStarted5.xaml"
                    ((global::Windows.UI.Xaml.Controls.Button)this.DoneButton).Click += this.GoNext;
                    #line default
                }
                break;
            default:
                break;
            }
            this._contentLoaded = true;
        }

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 14.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Windows.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Windows.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

