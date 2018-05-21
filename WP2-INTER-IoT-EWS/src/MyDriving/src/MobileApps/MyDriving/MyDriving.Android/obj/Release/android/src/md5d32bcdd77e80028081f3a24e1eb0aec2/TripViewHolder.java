package md5d32bcdd77e80028081f3a24e1eb0aec2;


public class TripViewHolder
	extends android.support.v7.widget.RecyclerView.ViewHolder
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("MyDriving.Droid.Fragments.TripViewHolder, MyDriving.Droid, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", TripViewHolder.class, __md_methods);
	}


	public TripViewHolder (android.view.View p0) throws java.lang.Throwable
	{
		super (p0);
		if (getClass () == TripViewHolder.class)
			mono.android.TypeManager.Activate ("MyDriving.Droid.Fragments.TripViewHolder, MyDriving.Droid, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Android.Views.View, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=84e04ff9cfb79065", this, new java.lang.Object[] { p0 });
	}

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
