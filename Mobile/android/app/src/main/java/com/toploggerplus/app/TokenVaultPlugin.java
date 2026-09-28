package com.toploggerplus.app;

import android.content.Context;
import android.content.SharedPreferences;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.CapacitorPlugin;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/** One encrypted token snapshot; the encryption key never leaves AndroidKeyStore. */
@CapacitorPlugin(name = "TokenVault")
public class TokenVaultPlugin extends Plugin {
    private static final String ALIAS = "TopLoggerPlusTokens";
    private SharedPreferences preferences() {
        return getContext().getSharedPreferences("token-vault", Context.MODE_PRIVATE);
    }
    private SecretKey key() throws Exception {
        KeyStore store = KeyStore.getInstance("AndroidKeyStore");
        store.load(null);
        if (store.containsAlias(ALIAS)) return (SecretKey) store.getKey(ALIAS, null);
        KeyGenerator generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore");
        generator.init(new KeyGenParameterSpec.Builder(ALIAS, KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
            .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
            .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
            .setRandomizedEncryptionRequired(true).build());
        return generator.generateKey();
    }
    @PluginMethod public synchronized void write(PluginCall call) {
        String value = call.getString("value");
        if (value == null || value.length() > 32768) { call.reject("Invalid token snapshot"); return; }
        try {
            Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
            cipher.init(Cipher.ENCRYPT_MODE, key());
            String encrypted = Base64.encodeToString(cipher.doFinal(value.getBytes(StandardCharsets.UTF_8)), Base64.NO_WRAP);
            String iv = Base64.encodeToString(cipher.getIV(), Base64.NO_WRAP);
            if (!preferences().edit().putString("encrypted", encrypted).putString("iv", iv).commit()) throw new Exception();
            call.resolve();
        } catch (Exception ignored) { call.reject("Could not securely save connection"); }
    }
    @PluginMethod public synchronized void read(PluginCall call) {
        try {
            String encrypted = preferences().getString("encrypted", null);
            JSObject result = new JSObject();
            if (encrypted != null) {
                String iv = preferences().getString("iv", null);
                if (iv == null) throw new Exception();
                Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
                cipher.init(Cipher.DECRYPT_MODE, key(), new GCMParameterSpec(128, Base64.decode(iv, Base64.NO_WRAP)));
                result.put("value", new String(cipher.doFinal(Base64.decode(encrypted, Base64.NO_WRAP)), StandardCharsets.UTF_8));
            }
            call.resolve(result);
        } catch (Exception ignored) { call.reject("Could not unlock connection"); }
    }
    @PluginMethod public synchronized void clear(PluginCall call) {
        try {
            if (!preferences().edit().clear().commit()) throw new Exception();
            KeyStore store = KeyStore.getInstance("AndroidKeyStore");
            store.load(null);
            if (store.containsAlias(ALIAS)) store.deleteEntry(ALIAS);
            call.resolve();
        } catch (Exception ignored) { call.reject("Could not remove connection"); }
    }
}
