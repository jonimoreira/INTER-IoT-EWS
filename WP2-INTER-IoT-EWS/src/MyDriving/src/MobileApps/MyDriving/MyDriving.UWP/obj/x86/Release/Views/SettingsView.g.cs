﻿#pragma checksum "D:\Projects\InterIOT\Workplan\WP2-INTER-IoT-EWS\src\MyDriving\src\MobileApps\MyDriving\MyDriving.UWP\Views\SettingsView.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "7729A1E01D5DCB834D887544E5BA1ED9"
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
    partial class SettingsView : 
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
                    this.TermsOfUseButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    #line 45 "..\..\..\Views\SettingsView.xaml"
                    ((global::Windows.UI.Xaml.Controls.Button)this.TermsOfUseButton).Click += this.TermsOfUseButton_Click;
                    #line default
                }
                break;
            case 2:
                {
                    this.PrivacyPolicyButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    #line 46 "..\..\..\Views\SettingsView.xaml"
                    ((global::Windows.UI.Xaml.Controls.Button)this.PrivacyPolicyButton).Click += this.PrivacyPolicyButton_Click;
                    #line default
                }
                break;
            case 3:
                {
                    this.OpenSourceNoticeButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    #line 47 "..\..\..\Views\SettingsView.xaml"
                    ((global::Windows.UI.Xaml.Controls.Button)this.OpenSourceNoticeButton).Click += this.OpenSourceNoticeButton_Click;
                    #line default
                }
                break;
            case 4:
                {
                    this.OpenSourceGitHubButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    #line 48 "..\..\..\Views\SettingsView.xaml"
                    ((global::Windows.UI.Xaml.Controls.Button)this.OpenSourceGitHubButton).Click += this.OpenSourceGitHubButton_Click;
                    #line default
                }
                break;
            case 5:
                {
                    this.Kilometers = (global::Windows.UI.Xaml.Controls.RadioButton)(target);
                }
                break;
            case 6:
                {
                    this.Miles = (global::Windows.UI.Xaml.Controls.RadioButton)(target);
                }
                break;
            case 7:
                {
                    this.Liters = (global::Windows.UI.Xaml.Controls.RadioButton)(target);
                }
                break;
            case 8:
                {
                    this.Gallons = (global::Windows.UI.Xaml.Controls.RadioButton)(target);
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

