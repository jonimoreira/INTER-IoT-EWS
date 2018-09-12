package mono.android.support.v7.preference;


public class Preference_OnPreferenceChangeListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.support.v7.preference.Preference.OnPreferenceChangeListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onPreferenceChange:(Landroid/support/v7/preference/Preference;Ljava/lang/Object;)Z:GetOnPreferenceChange_Landroid_support_v7_preference_Preference_Ljava_lang_Object_Handler:Android.Support.V7.Preferences.Preference/IOnPreferenceChangeListenerInvoker, Xamarin.Android.Support.v7.Preference\n" +
			"";
		mono.android.Runtime.register ("Android.Support.V7.Preferences.Preference+IOnPreferenceChangeListenerImplementor, Xamarin.Android.Support.v7.Preference, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", Preference_OnPreferenceChangeListenerImplementor.class, __md_methods);
	}


	public Preference_OnPreferenceChangeListenerImplementor () throws java.lang.Throwable
	{
		super ();
		if (getClass () == Preference_OnPreferenceChangeListenerImplementor.class)
			mono.android.TypeManager.Activate ("Android.Support.V7.Preferences.Preference+IOnPreferenceChangeListenerImplementor, Xamarin.Android.Support.v7.Preference, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}


	public boolean onPreferenceChange (android.support.v7.preference.Preference p0, java.lang.Object p1)
	{
		return n_onPreferenceChange (p0, p1);
	}

	private native boolean n_onPreferenceChange (android.support.v7.preference.Preference p0, java.lang.Object p1);

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
