package mono;

import java.io.*;
import java.lang.String;
import java.util.Locale;
import java.util.HashSet;
import java.util.zip.*;
import android.content.Context;
import android.content.Intent;
import android.content.pm.ApplicationInfo;
import android.content.res.AssetManager;
import android.util.Log;
import mono.android.Runtime;

public class MonoPackageManager {

	static Object lock = new Object ();
	static boolean initialized;

	static android.content.Context Context;

	public static void LoadApplication (Context context, ApplicationInfo runtimePackage, String[] apks)
	{
		synchronized (lock) {
			if (context instanceof android.app.Application) {
				Context = context;
			}
			if (!initialized) {
				android.content.IntentFilter timezoneChangedFilter  = new android.content.IntentFilter (
						android.content.Intent.ACTION_TIMEZONE_CHANGED
				);
				context.registerReceiver (new mono.android.app.NotifyTimeZoneChanges (), timezoneChangedFilter);
				
				System.loadLibrary("monodroid");
				Locale locale       = Locale.getDefault ();
				String language     = locale.getLanguage () + "-" + locale.getCountry ();
				String filesDir     = context.getFilesDir ().getAbsolutePath ();
				String cacheDir     = context.getCacheDir ().getAbsolutePath ();
				String dataDir      = getNativeLibraryPath (context);
				ClassLoader loader  = context.getClassLoader ();

				Runtime.init (
						language,
						apks,
						getNativeLibraryPath (runtimePackage),
						new String[]{
							filesDir,
							cacheDir,
							dataDir,
						},
						loader,
						new java.io.File (
							android.os.Environment.getExternalStorageDirectory (),
							"Android/data/" + context.getPackageName () + "/files/.__override__").getAbsolutePath (),
						MonoPackageManager_Resources.Assemblies,
						context.getPackageName ());
				
				mono.android.app.ApplicationRegistration.registerApplications ();
				
				initialized = true;
			}
		}
	}

	public static void setContext (Context context)
	{
		// Ignore; vestigial
	}

	static String getNativeLibraryPath (Context context)
	{
	    return getNativeLibraryPath (context.getApplicationInfo ());
	}

	static String getNativeLibraryPath (ApplicationInfo ainfo)
	{
		if (android.os.Build.VERSION.SDK_INT >= 9)
			return ainfo.nativeLibraryDir;
		return ainfo.dataDir + "/lib";
	}

	public static String[] getAssemblies ()
	{
		return MonoPackageManager_Resources.Assemblies;
	}

	public static String[] getDependencies ()
	{
		return MonoPackageManager_Resources.Dependencies;
	}

	public static String getApiPackageName ()
	{
		return MonoPackageManager_Resources.ApiPackageName;
	}
}

class MonoPackageManager_Resources {
	public static final String[] Assemblies = new String[]{
		/* We need to ensure that "ShimmerCaptureXamarin.dll" comes first in this list. */
		"ShimmerCaptureXamarin.dll",
		"Google.Protobuf.dll",
		"json-ld.net.dll",
		"Microsoft.Azure.Amqp.dll",
		"Microsoft.Azure.Devices.Client.PCL.dll",
		"Newtonsoft.Json.dll",
		"PCLCrypto.dll",
		"PerpetualEngine.SimpleStorage.Droid.dll",
		"Plugin.CurrentActivity.dll",
		"Plugin.Geolocator.Abstractions.dll",
		"Plugin.Geolocator.dll",
		"Plugin.Permissions.Abstractions.dll",
		"Plugin.Permissions.dll",
		"Validation.dll",
		"Xamarin.Android.Support.v4.dll",
		"XPlat.Core.dll",
		"XPlat.Core.Droid.dll",
		"ShimmerAPI.dll",
		"System.Runtime.dll",
		"System.IO.dll",
		"System.Threading.Tasks.dll",
		"System.Text.Encoding.dll",
		"System.Diagnostics.Debug.dll",
		"System.Collections.dll",
		"System.Text.RegularExpressions.dll",
		"System.Reflection.dll",
		"System.ObjectModel.dll",
		"System.Linq.Expressions.dll",
		"System.Runtime.Extensions.dll",
		"System.Globalization.dll",
		"System.Linq.dll",
		"System.Threading.dll",
		"System.Reflection.Extensions.dll",
		"System.Security.Cryptography.Algorithms.dll",
		"System.Security.Cryptography.Primitives.dll",
		"System.Resources.ResourceManager.dll",
		"System.Runtime.InteropServices.dll",
		"System.Collections.Concurrent.dll",
		"System.Runtime.InteropServices.WindowsRuntime.dll",
		"System.Net.Primitives.dll",
		"System.Net.Requests.dll",
		"System.Runtime.Numerics.dll",
		"System.Xml.XmlDocument.dll",
		"System.Xml.XDocument.dll",
		"System.Runtime.Serialization.Primitives.dll",
		"System.ComponentModel.TypeConverter.dll",
		"System.Dynamic.Runtime.dll",
		"System.Runtime.Serialization.Formatters.dll",
		"System.Xml.ReaderWriter.dll",
		"System.Reflection.Primitives.dll",
		"System.Text.Encoding.Extensions.dll",
	};
	public static final String[] Dependencies = new String[]{
	};
	public static final String ApiPackageName = "Mono.Android.Platform.ApiLevel_23";
}
