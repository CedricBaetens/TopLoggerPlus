package com.toploggerplus.app;

import android.app.Activity;
import android.content.Intent;
import androidx.activity.result.ActivityResult;
import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.ActivityCallback;
import com.getcapacitor.annotation.CapacitorPlugin;

@CapacitorPlugin(name = "TopLoggerLogin")
public class TopLoggerLoginPlugin extends Plugin {
    @PluginMethod public void open(PluginCall call) {
        startActivityForResult(call, new Intent(getContext(), TopLoggerLoginActivity.class), "loginResult");
    }
    @ActivityCallback private void loginResult(PluginCall call, ActivityResult result) {
        if (call == null) return;
        String token = result.getData() == null ? null : result.getData().getStringExtra("refreshToken");
        if (result.getResultCode() == Activity.RESULT_OK && token != null && token.matches("[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+")) {
            call.resolve(new JSObject().put("refreshToken", token));
        } else call.reject("Sign-in cancelled. You can also connect with a refresh token.");
    }
}
