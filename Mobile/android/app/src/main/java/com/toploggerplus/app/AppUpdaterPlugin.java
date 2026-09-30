package com.toploggerplus.app;

import android.content.Intent;
import android.content.pm.PackageInfo;
import android.net.Uri;
import android.os.Build;
import android.provider.Settings;
import androidx.activity.result.ActivityResult;
import androidx.core.content.FileProvider;
import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.ActivityCallback;
import com.getcapacitor.annotation.CapacitorPlugin;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

/** Downloads a release APK from this project's GitHub releases into the app cache and hands it to Android's installer. */
@CapacitorPlugin(name = "AppUpdater")
public class AppUpdaterPlugin extends Plugin {
    static final String RELEASES = "/CedricBaetens/TopLoggerPlus/releases/download/";
    private static final long MAX_BYTES = 100L * 1024 * 1024;
    private final ExecutorService executor = Executors.newSingleThreadExecutor();

    static boolean releaseUrl(String url) {
        if (url == null) return false;
        Uri uri = Uri.parse(url);
        String path = uri.getPath();
        return "https".equals(uri.getScheme()) && "github.com".equals(uri.getHost()) && uri.getPort() == -1
            && path != null && path.startsWith(RELEASES) && path.endsWith(".apk") && !path.contains("..");
    }
    private File folder() { return new File(getContext().getCacheDir(), "updates"); }
    private void deleteDownloads() {
        File[] files = folder().listFiles();
        if (files != null) for (File file : files) file.delete();
    }
    private boolean mayInstall() {
        return Build.VERSION.SDK_INT < Build.VERSION_CODES.O || getContext().getPackageManager().canRequestPackageInstalls();
    }

    // A new process means any earlier update was installed or abandoned.
    @Override public void load() { executor.execute(this::deleteDownloads); }

    @PluginMethod public void install(PluginCall call) {
        if (!releaseUrl(call.getString("url"))) { call.reject("Not a TopLogger Plus release."); return; }
        if (mayInstall()) { download(call); return; }
        startActivityForResult(call, new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES, Uri.parse("package:" + getContext().getPackageName())), "permissionResult");
    }
    @ActivityCallback private void permissionResult(PluginCall call, ActivityResult result) {
        if (call == null) return;
        if (mayInstall()) download(call);
        else call.reject("Allow TopLogger Plus to install updates, then try again.", "PERMISSION");
    }

    private void download(PluginCall call) {
        String url = call.getString("url");
        executor.execute(() -> {
            HttpURLConnection connection = null;
            try {
                deleteDownloads();
                File folder = folder(), apk = new File(folder, "update.apk");
                if (!folder.isDirectory() && !folder.mkdirs()) throw new IOException("No cache folder");
                // GitHub redirects release downloads to its https asset host; HttpURLConnection follows that.
                connection = (HttpURLConnection) new URL(url).openConnection();
                connection.setConnectTimeout(15000); connection.setReadTimeout(30000);
                if (connection.getResponseCode() != HttpURLConnection.HTTP_OK) throw new IOException("HTTP " + connection.getResponseCode());
                long total = connection.getContentLengthLong(), done = 0;
                if (total > MAX_BYTES) throw new IOException("Too large");
                try (InputStream in = connection.getInputStream(); OutputStream out = new FileOutputStream(apk)) {
                    byte[] buffer = new byte[65536];
                    int read, reported = -2;
                    while ((read = in.read(buffer)) != -1) {
                        if ((done += read) > MAX_BYTES) throw new IOException("Too large");
                        out.write(buffer, 0, read);
                        int percent = total > 0 ? (int) (done * 100 / total) : -1;
                        if (percent != reported) notifyListeners("progress", new JSObject().put("percent", reported = percent));
                    }
                }
                PackageInfo info = getContext().getPackageManager().getPackageArchiveInfo(apk.getPath(), 0);
                if (info == null || !getContext().getPackageName().equals(info.packageName)) throw new IOException("Not this app");
                Uri uri = FileProvider.getUriForFile(getContext(), getContext().getPackageName() + ".fileprovider", apk);
                Intent intent = new Intent(Intent.ACTION_VIEW).setDataAndType(uri, "application/vnd.android.package-archive")
                    .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                getActivity().runOnUiThread(() -> {
                    try { getActivity().startActivity(intent); call.resolve(); }
                    catch (Exception ignored) { call.reject("Android could not open the installer."); }
                });
            } catch (Exception ignored) {
                deleteDownloads();
                call.reject("The update could not be downloaded. Check your connection and try again.");
            } finally { if (connection != null) connection.disconnect(); }
        });
    }
}
