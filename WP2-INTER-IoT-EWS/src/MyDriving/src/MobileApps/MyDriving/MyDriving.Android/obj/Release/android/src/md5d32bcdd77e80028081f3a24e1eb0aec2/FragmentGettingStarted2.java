package md5d32bcdd77e80028081f3a24e1eb0aec2;


public class FragmentGettingStarted2
	extends android.support.v4.app.Fragment
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onCreateView:(Landroid/view/LayoutInflater;Landroid/view/ViewGroup;Landroid/os/Bundle;)Landroid/view/View;:GetOnCreateView_Landroid_view_LayoutInflater_Landroid_view_ViewGroup_Landroid_os_Bundle_Handler\n" +
			"";
		mono.android.Runtime.register ("MyDriving.Droid.Fragments.FragmentGettingStarted2, MyDriving.Droid, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", FragmentGettingStarted2.class, __md_methods);
	}


	public FragmentGettingStarted2 () throws java.lang.Throwable
	{
		super ();
		if (getClass () == FragmentGettingStarted2.class)
			mono.android.TypeManager.Activate ("MyDriving.Droid.Fragments.FragmentGettingStarted2, MyDriving.Droid, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}


	public android.view.View onCreateView (android.view.LayoutInflater p0, android.view.ViewGroup p1, android.os.Bundle p2)
	{
		return n_onCreateView (p0, p1, p2);
	}

	private native android.view.View n_onCreateView (android.view.LayoutInflater p0, android.view.ViewGroup p1, android.os.Bundle p2);

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
