window.habitFlowAuth = {
  login: async function (baseUrl, email, password) {
    const response = await fetch(`${baseUrl}/api/v1/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      credentials: "include",
      body: JSON.stringify({ email, password })
    });

    let message = "";
    try {
      const payload = await response.json();
      message = payload?.detail ?? payload?.title ?? "";
    } catch {
      message = "";
    }

    return { ok: response.ok, status: response.status, message: message };
  },

  logout: async function (baseUrl) {
    const response = await fetch(`${baseUrl}/api/v1/auth/logout`, {
      method: "POST",
      credentials: "include"
    });

    let message = "";
    try {
      const payload = await response.json();
      message = payload?.detail ?? payload?.title ?? "";
    } catch {
      message = "";
    }

    return { ok: response.ok, status: response.status, message: message };
  }
};
