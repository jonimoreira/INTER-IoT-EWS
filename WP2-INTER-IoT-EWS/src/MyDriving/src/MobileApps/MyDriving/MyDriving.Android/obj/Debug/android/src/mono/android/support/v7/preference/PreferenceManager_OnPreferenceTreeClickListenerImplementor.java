package mono.android.support.v7.preference;


public class PreferenceManager_OnPreferenceTreeClickListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.support.v7.preference.PreferenceManager.OnPreferenceTreeClickListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onPreferenceTreeClick:(Landroid/support/v7/preference/Preference;)Z:GetOnPreferenceTreeClick_Landroid_support_v7_preference_Preference_Handler:Android.Support.V7.Preferences.PreferenceManager/IOnPreferenceTreeClickListenerInvoker, Xamarin.Android.Support.v7.Preference\n" +
			"";
		mono.android.Runtime.register ("Android.Support.V7.Preferences.PreferenceManager+IOnPreferenceTreeClickListenerImplementor, Xamarin.Android.Support.v7.Preference, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", PreferenceManager_OnPreferenceTreeClickListenerImplementor.class, __md_methods);
	}


	public PreferenceManager_OnPreferenceTreeClickListenerImplementor () throws java.lang.Throwable
	{
		super ();
		if (getClass () == PreferenceManager_OnPreferenceTreeClickListenerImplementor.class)
			mono.android.TypeManager.Activate ("Android.Support.V7.Preferences.PreferenceManager+IOnPreferenceTreeClickListenerImplementor, Xamarin.Android.Support.v7.Preference, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}


	public boolean onPreferenceTreeClick (android.support.v7.preference.Preference p0)
	{
		return n_onPreferenceTreeClick (p0);
	}

	private native boolean n_onPreferenceTreeClick (android.support.v7.preference.Preference p0);

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
