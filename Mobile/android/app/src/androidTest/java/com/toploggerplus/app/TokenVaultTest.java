package com.toploggerplus.app;

import static org.junit.Assert.*;
import android.content.Context;
import android.content.SharedPreferences;
import androidx.test.ext.junit.runners.AndroidJUnit4;
import androidx.test.platform.app.InstrumentationRegistry;
import com.getcapacitor.JSObject;
import com.getcapacitor.PluginCall;
import org.junit.Test;
import org.junit.runner.RunWith;

@RunWith(AndroidJUnit4.class)
public class TokenVaultTest {
    @Test public void loginOriginIsRestricted() {
        assertTrue(TopLoggerLoginActivity.officialUrl("https://app.toplogger.nu/en/sign-in"));
        assertTrue(TopLoggerLoginActivity.officialUrl("https://app.toplogger.nu:443/"));
        assertFalse(TopLoggerLoginActivity.officialUrl("http://app.toplogger.nu/"));
        assertFalse(TopLoggerLoginActivity.officialUrl("https://app.toplogger.nu.evil.example/"));
        assertFalse(TopLoggerLoginActivity.officialUrl("https://app.toplogger.nu:8443/"));
        assertFalse(TopLoggerLoginActivity.officialUrl(null));
    }
    @Test public void loginCapturesOfficialSession() throws Exception {
        android.app.Instrumentation instrumentation = InstrumentationRegistry.getInstrumentation();
        Context context = instrumentation.getTargetContext();
        assertTrue(context.getPackageName().endsWith(".qa"));
        android.content.Intent intent = new android.content.Intent(context, TopLoggerLoginActivity.class).addFlags(android.content.Intent.FLAG_ACTIVITY_NEW_TASK);
        TopLoggerLoginActivity activity = (TopLoggerLoginActivity) instrumentation.startActivitySync(intent);
        try {
            java.lang.reflect.Field field = TopLoggerLoginActivity.class.getDeclaredField("web"); field.setAccessible(true);
            android.webkit.WebView web = (android.webkit.WebView) field.get(activity);
            instrumentation.runOnMainSync(() -> {
                web.stopLoading();
                web.loadDataWithBaseURL(TopLoggerLoginActivity.ORIGIN + "/", "<script>localStorage.setItem('tl-auth', JSON.stringify({refresh:{token:'test.payload.signature'}}))</script>", "text/html", "UTF-8", TopLoggerLoginActivity.ORIGIN + "/");
            });
            long deadline = System.currentTimeMillis() + 8000;
            while (!activity.isFinishing() && System.currentTimeMillis() < deadline) Thread.sleep(100);
            assertTrue("Official session should complete native sign-in", activity.isFinishing());
        } finally { instrumentation.runOnMainSync(activity::finish); }
    }
    private static class Call extends PluginCall {
        JSObject result;
        String failure;
        boolean resolved;
        Call(JSObject data) { super(null, "TokenVault", "test", "test", data); }
        @Override public void resolve(JSObject data) { result = data; resolved = true; }
        @Override public void resolve() { resolved = true; }
        @Override public void reject(String message) { failure = message; }
    }
    @Test public void encryptedRoundTripTamperAndClear() {
        Context context = InstrumentationRegistry.getInstrumentation().getTargetContext();
        assertTrue(context.getPackageName().endsWith(".qa"));
        TokenVaultPlugin vault = new TokenVaultPlugin() { @Override public Context getContext() { return context; } };
        SharedPreferences prefs = context.getSharedPreferences("token-vault", Context.MODE_PRIVATE);
        try {
            Call clear = new Call(new JSObject()); vault.clear(clear); assertTrue(clear.resolved);
            String value = "test-token-snapshot";
            Call write = new Call(new JSObject().put("value", value)); vault.write(write); assertNull(write.failure); assertTrue(write.resolved);
            assertFalse(prefs.getAll().toString().contains(value));
            String ciphertext = prefs.getString("encrypted", null);
            assertNotNull(ciphertext);
            Call read = new Call(new JSObject()); vault.read(read); assertNull(read.failure); assertEquals(value, read.result.getString("value"));
            Call overwrite = new Call(new JSObject().put("value", value)); vault.write(overwrite); assertTrue(overwrite.resolved);
            assertNotEquals(ciphertext, prefs.getString("encrypted", null));
            assertTrue(prefs.edit().putString("encrypted", "tampered").commit());
            Call corrupted = new Call(new JSObject()); vault.read(corrupted); assertNotNull(corrupted.failure); assertFalse(corrupted.resolved);
            Call remove = new Call(new JSObject()); vault.clear(remove); assertTrue(remove.resolved);
            Call absent = new Call(new JSObject()); vault.read(absent); assertNull(absent.failure); assertNull(absent.result.getString("value"));
        } finally { vault.clear(new Call(new JSObject())); }
    }
}
