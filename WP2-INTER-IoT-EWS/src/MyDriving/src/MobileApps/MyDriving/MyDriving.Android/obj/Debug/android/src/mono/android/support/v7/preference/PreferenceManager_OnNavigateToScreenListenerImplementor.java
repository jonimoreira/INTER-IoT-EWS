package mono.android.support.v7.preference;


public class PreferenceManager_OnNavigateToScreenListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.support.v7.preference.PreferenceManager.OnNavigateToScreenListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onNavigateToScreen:(Landroid/support/v7/preference/PreferenceScreen;)V:GetOnNavigateToScreen_Landroid_support_v7_preference_PreferenceScreen_Handler:Android.Support.V7.Preferences.PreferenceManager/IOnNavigateToScreenListenerInvoker, Xamarin.Android.Support.v7.Preference\n" +
			"";
		mono.android.Runtime.register ("Android.Support.V7.Preferences.PreferenceManager+IOnNavigateToScreenListenerImplementor, Xamarin.Android.Support.v7.Preference, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", PreferenceManager_OnNavigateToScreenListenerImplementor.class, __md_methods);
	}


	public PreferenceManager_OnNavigateToScreenListenerImplementor () throws java.lang.Throwable
	{
		super ();
		if (getClass () == PreferenceManager_OnNavigateToScreenListenerImplementor.class)
			mono.android.TypeManager.Activate ("Android.Support.V7.Preferences.PreferenceManager+IOnNavigateToScreenListenerImplementor, Xamarin.Android.Support.v7.Preference, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}


	public void onNavigateToScreen (android.support.v7.preference.PreferenceScreen p0)
	{
		n_onNavigateToScreen (p0);
	}

	private native void n_onNavigateToScreen (android.support.v7.preference.PreferenceScreen p0);

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
