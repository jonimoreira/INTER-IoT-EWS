package mono.android.support.v7.preference;


public class Preference_OnPreferenceClickListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.support.v7.preference.Preference.OnPreferenceClickListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onPreferenceClick:(Landroid/support/v7/preference/Preference;)Z:GetOnPreferenceClick_Landroid_support_v7_preference_Preference_Handler:Android.Support.V7.Preferences.Preference/IOnPreferenceClickListenerInvoker, Xamarin.Android.Support.v7.Preference\n" +
			"";
		mono.android.Runtime.register ("Android.Support.V7.Preferences.Preference+IOnPreferenceClickListenerImplementor, Xamarin.Android.Support.v7.Preference, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", Preference_OnPreferenceClickListenerImplementor.class, __md_methods);
	}


	public Preference_OnPreferenceClickListenerImplementor () throws java.lang.Throwable
	{
		super ();
		if (getClass () == Preference_OnPreferenceClickListenerImplementor.class)
			mono.android.TypeManager.Activate ("Android.Support.V7.Preferences.Preference+IOnPreferenceClickListenerImplementor, Xamarin.Android.Support.v7.Preference, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}


	public boolean onPreferenceClick (android.support.v7.preference.Preference p0)
	{
		return n_onPreferenceClick (p0);
	}

	private native boolean n_onPreferenceClick (android.support.v7.preference.Preference p0);

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
