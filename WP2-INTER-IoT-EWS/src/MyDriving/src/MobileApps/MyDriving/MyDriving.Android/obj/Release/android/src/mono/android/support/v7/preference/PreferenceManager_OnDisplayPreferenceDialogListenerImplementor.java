package mono.android.support.v7.preference;


public class PreferenceManager_OnDisplayPreferenceDialogListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.support.v7.preference.PreferenceManager.OnDisplayPreferenceDialogListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onDisplayPreferenceDialog:(Landroid/support/v7/preference/Preference;)V:GetOnDisplayPreferenceDialog_Landroid_support_v7_preference_Preference_Handler:Android.Support.V7.Preferences.PreferenceManager/IOnDisplayPreferenceDialogListenerInvoker, Xamarin.Android.Support.v7.Preference\n" +
			"";
		mono.android.Runtime.register ("Android.Support.V7.Preferences.PreferenceManager+IOnDisplayPreferenceDialogListenerImplementor, Xamarin.Android.Support.v7.Preference, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", PreferenceManager_OnDisplayPreferenceDialogListenerImplementor.class, __md_methods);
	}


	public PreferenceManager_OnDisplayPreferenceDialogListenerImplementor () throws java.lang.Throwable
	{
		super ();
		if (getClass () == PreferenceManager_OnDisplayPreferenceDialogListenerImplementor.class)
			mono.android.TypeManager.Activate ("Android.Support.V7.Preferences.PreferenceManager+IOnDisplayPreferenceDialogListenerImplementor, Xamarin.Android.Support.v7.Preference, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}


	public void onDisplayPreferenceDialog (android.support.v7.preference.Preference p0)
	{
		n_onDisplayPreferenceDialog (p0);
	}

	private native void n_onDisplayPreferenceDialog (android.support.v7.preference.Preference p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
