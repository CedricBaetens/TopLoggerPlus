FROM eclipse-temurin:21-jdk-noble AS jdk

FROM node:22-bookworm

ENV JAVA_HOME=/opt/java/openjdk \
    ANDROID_HOME=/opt/android-sdk \
    ANDROID_SDK_ROOT=/opt/android-sdk
ENV PATH=$JAVA_HOME/bin:$ANDROID_HOME/cmdline-tools/latest/bin:$ANDROID_HOME/platform-tools:$PATH

COPY --from=jdk /opt/java/openjdk /opt/java/openjdk

RUN apt-get update \
    && apt-get install -y --no-install-recommends unzip \
    && rm -rf /var/lib/apt/lists/*

# Optional extra CAs (e.g. a TLS-inspecting corporate proxy): drop *.crt files in .devcontainer/certs/
COPY certs/ /usr/local/share/ca-certificates/extra/
RUN update-ca-certificates \
    && for crt in /usr/local/share/ca-certificates/extra/*.crt; do \
        [ -e "$crt" ] || continue; \
        keytool -importcert -noprompt -cacerts -storepass changeit -alias "$(basename "$crt" .crt)" -file "$crt"; \
    done
ENV NODE_EXTRA_CA_CERTS=/etc/ssl/certs/ca-certificates.crt

# Checksum from https://dl.google.com/android/repository/repository2-3.xml
ARG CMDLINE_TOOLS=commandlinetools-linux-16111833_latest.zip
ARG CMDLINE_TOOLS_SHA1=e025545c62a8e64c7559119566a569fb1dec5f60
RUN mkdir -p $ANDROID_HOME/cmdline-tools \
    && curl -fsSL -o /tmp/tools.zip https://dl.google.com/android/repository/$CMDLINE_TOOLS \
    && echo "$CMDLINE_TOOLS_SHA1  /tmp/tools.zip" | sha1sum -c - \
    && unzip -q /tmp/tools.zip -d /tmp/tools \
    && mv /tmp/tools/cmdline-tools $ANDROID_HOME/cmdline-tools/latest \
    && rm -rf /tmp/tools /tmp/tools.zip \
    && yes | sdkmanager --licenses > /dev/null \
    && sdkmanager "platform-tools" "platforms;android-36" "build-tools;36.0.0" > /dev/null \
    && chown -R node:node $ANDROID_HOME

RUN npm install -g npm@11 && npm cache clean --force

USER node

# Keep caches inside the workspace so nothing ends up in Docker volumes or the host home directory
ENV GRADLE_USER_HOME=/workspaces/TopLoggerPlus/.cache/gradle \
    npm_config_cache=/workspaces/TopLoggerPlus/.cache/npm \
    CLAUDE_CONFIG_DIR=/workspaces/TopLoggerPlus/.cache/claude \
    GH_CONFIG_DIR=/workspaces/TopLoggerPlus/.cache/gh \
    GIT_CONFIG_GLOBAL=/workspaces/TopLoggerPlus/.cache/git/config \
    GIT_CONFIG_NOSYSTEM=1
