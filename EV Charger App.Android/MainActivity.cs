using Android.App;
using Android.Content.PM;
using Android.Gms.Common.Api.Internal;
using Android.OS;
using Android.Runtime;

namespace EV_Charger_App.Droid
{
    [Activity(Label = "EV_Charger_App", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            Xamarin.FormsGoogleMaps.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            App app = new App();
            string apiKey = app.database.GetGoogleAPIKey();

            // Modify the AndroidManifest.xml file to set the API key value dynamically
            var context = Android.App.Application.Context;
            var packageManager = context.PackageManager;
            var packageName = context.PackageName;
            var applicationInfo = packageManager.GetApplicationInfo(packageName, PackageInfoFlags.MetaData);
            var metaData = applicationInfo.MetaData;
            metaData.PutString("com.google.android.maps.v2.API_KEY", apiKey);

            LoadApplication(app);
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}