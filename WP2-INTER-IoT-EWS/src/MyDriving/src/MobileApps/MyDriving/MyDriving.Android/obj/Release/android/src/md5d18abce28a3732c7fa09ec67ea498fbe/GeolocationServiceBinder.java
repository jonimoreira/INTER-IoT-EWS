package md5d18abce28a3732c7fa09ec67ea498fbe;


public class GeolocationServiceBinder
	extends android.os.Binder
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("MyDriving.Droid.Services.GeolocationServiceBinder, MyDriving.Droid, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", GeolocationServiceBinder.class, __md_methods);
	}


	public GeolocationServiceBinder () throws java.lang.Throwable
	{
		super ();
		if (getClass () == GeolocationServiceBinder.class)
			mono.android.TypeManager.Activate ("MyDriving.Droid.Services.GeolocationServiceBinder, MyDriving.Droid, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}

	public GeolocationServiceBinder (md5d18abce28a3732c7fa09ec67ea498fbe.GeolocationService p0) throws java.lang.Throwable
	{
		super ();
		if (getClass () == GeolocationServiceBinder.class)
			mono.android.TypeManager.Activate ("MyDriving.Droid.Services.GeolocationServiceBinder, MyDriving.Droid, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "MyDriving.Droid.Services.GeolocationService, MyDriving.Droid, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", this, new java.lang.Object[] { p0 });
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
