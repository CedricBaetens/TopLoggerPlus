package com.toploggerplus.app;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.Intent;
import android.graphics.Color;
import android.net.Uri;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.View;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebStorage;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;
import androidx.activity.OnBackPressedCallback;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.view.ViewCompat;
import androidx.core.view.WindowInsetsCompat;
import org.json.JSONTokener;

/** Official TopLogger web sign-in. No password interception or JavaScript/native bridge. */
public class TopLoggerLoginActivity extends AppCompatActivity {
    static final String ORIGIN = "https://app.toplogger.nu";
    private final Handler handler = new Handler(Looper.getMainLooper());
    private WebView web;
    private boolean closing;
    private TextView title;
    static boolean officialUrl(String url) {
        if (url == null) return false;
        Uri uri = Uri.parse(url);
        return "https".equals(uri.getScheme()) && "app.toplogger.nu".equals(uri.getHost()) && (uri.getPort() == -1 || uri.getPort() == 443);
    }
    private final Runnable poll = new Runnable() {
        @Override public void run() {
            if (closing || web == null) return;
            if (!officialUrl(web.getUrl())) { handler.postDelayed(this, 800); return; }
            web.evaluateJavascript("(() => { if (location.origin !== 'https://app.toplogger.nu') return null; try { const auth = JSON.parse(localStorage.getItem('tl-auth') || 'null'); return auth?.refresh?.token || null; } catch { return null; } })()", result -> {
                if (closing || web == null || !officialUrl(web.getUrl())) return;
                try {
                    Object value = new JSONTokener(result).nextValue();
                    if (value instanceof String && ((String) value).matches("[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+")) { finishLogin((String) value); return; }
                } catch (Exception ignored) { /* No session yet. */ }
                handler.postDelayed(this, 800);
            });
        }
    };
    @SuppressLint("SetJavaScriptEnabled")
    @Override public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        LinearLayout root = new LinearLayout(this); root.setOrientation(LinearLayout.VERTICAL); root.setBackgroundColor(Color.rgb(24, 59, 48));
        LinearLayout toolbar = new LinearLayout(this); toolbar.setPadding(12, 4, 12, 4); toolbar.setGravity(android.view.Gravity.CENTER_VERTICAL);
        Button close = new Button(this); close.setText("Close"); close.setContentDescription("Close TopLogger sign-in"); close.setOnClickListener(view -> finishLogin(null));
        title = new TextView(this); title.setText("TopLogger · app.toplogger.nu"); title.setTextColor(Color.WHITE); title.setTextSize(14); title.setPadding(16, 8, 4, 8);
        toolbar.addView(close); toolbar.addView(title, new LinearLayout.LayoutParams(0, -2, 1)); root.addView(toolbar);
        ProgressBar progress = new ProgressBar(this, null, android.R.attr.progressBarStyleHorizontal); progress.setMax(100); root.addView(progress, new LinearLayout.LayoutParams(-1, 6));
        web = new WebView(this);
        web.getSettings().setJavaScriptEnabled(true); web.getSettings().setDomStorageEnabled(true);
        web.getSettings().setAllowFileAccess(false); web.getSettings().setAllowContentAccess(false);
        web.getSettings().setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        web.getSettings().setSaveFormData(false);
        web.setWebChromeClient(new WebChromeClient() { @Override public void onProgressChanged(WebView view, int value) { progress.setProgress(value); progress.setVisibility(value == 100 ? View.GONE : View.VISIBLE); } });
        web.setWebViewClient(new WebViewClient() {
            @Override public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                if (!request.isForMainFrame()) return false;
                if (officialUrl(request.getUrl().toString())) return false;
                title.setText("Use email sign-in here, or a refresh token in Plus.");
                return true;
            }
        });
        root.addView(web, new LinearLayout.LayoutParams(-1, 0, 1)); setContentView(root);
        ViewCompat.setOnApplyWindowInsetsListener(root, (view, insets) -> {
            androidx.core.graphics.Insets bars = insets.getInsets(WindowInsetsCompat.Type.systemBars() | WindowInsetsCompat.Type.ime());
            view.setPadding(bars.left, bars.top, bars.right, bars.bottom); return insets;
        });
        getOnBackPressedDispatcher().addCallback(this, new OnBackPressedCallback(true) { @Override public void handleOnBackPressed() { finishLogin(null); } });
        web.loadUrl(ORIGIN + "/en/sign-in"); handler.postDelayed(poll, 800);
    }
    private void finishLogin(String token) {
        if (closing) return;
        closing = true; handler.removeCallbacks(poll);
        // Remove the official site's plaintext session before returning to Plus's secure vault.
        Runnable complete = () -> {
            WebStorage.getInstance().deleteOrigin(ORIGIN);
            if (token != null) setResult(Activity.RESULT_OK, new Intent().putExtra("refreshToken", token));
            else setResult(Activity.RESULT_CANCELED);
            finish();
        };
        if (officialUrl(web.getUrl())) web.evaluateJavascript("localStorage.removeItem('tl-auth'); sessionStorage.clear();", ignored -> complete.run());
        else complete.run();
    }
    @Override protected void onDestroy() {
        handler.removeCallbacksAndMessages(null);
        if (web != null) { web.stopLoading(); web.loadUrl("about:blank"); web.destroy(); web = null; }
        super.onDestroy();
    }
}
