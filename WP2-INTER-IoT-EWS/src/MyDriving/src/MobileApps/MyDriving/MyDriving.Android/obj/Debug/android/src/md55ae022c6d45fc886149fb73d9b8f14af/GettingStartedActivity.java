package md55ae022c6d45fc886149fb73d9b8f14af;


public class GettingStartedActivity
	extends md5130d33284058cd7daf1ce69e066bb8a5.BaseActivity
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onCreate:(Landroid/os/Bundle;)V:GetOnCreate_Landroid_os_Bundle_Handler\n" +
			"n_onBackPressed:()V:GetOnBackPressedHandler\n" +
			"";
		mono.android.Runtime.register ("MyDriving.Droid.Activities.GettingStartedActivity, MyDriving.Droid, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", GettingStartedActivity.class, __md_methods);
	}


	public GettingStartedActivity () throws java.lang.Throwable
	{
		super ();
		if (getClass () == GettingStartedActivity.class)
			mono.android.TypeManager.Activate ("MyDriving.Droid.Activities.GettingStartedActivity, MyDriving.Droid, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}


	public void onCreate (android.os.Bundle p0)
	{
		n_onCreate (p0);
	}

	private native void n_onCreate (android.os.Bundle p0);


	public void onBackPressed ()
	{
		n_onBackPressed ();
	}

	private native void n_onBackPressed ();

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
