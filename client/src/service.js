import axios from 'axios';
import { jwtDecode } from "jwt-decode";

//הגדרת כתובת API כברירית מחדל
axios.defaults.baseURL = process.env.REACT_APP_API_URL;

// הוספת Interceptor כדי לטפל בשגיאות API באופן מרכזי
axios.interceptors.response.use(
  response => response, // אם הכל בסדר, פשוט תעביר את התגובה הלאה.
  error => {
    if (error.response && error.response.status === 401) {
      console.log("Not authenticated - please login");
      // הסרנו את הניתוב כי עכשיו App.js מטפל בזה
    }
    else {
      // אם יש שגיאה, נדפיס אותה ללוג (Console).
      console.error("API Error detected:", error.response?.data || error.message);
    }
    return Promise.reject(error); // זורק את השגיאה הלאה למי שקרא לפונקציה.
  }
);
setAuthorizationBearer();

function saveAccessToken(authResult) {
  localStorage.setItem("access_token", authResult.token);
  setAuthorizationBearer();
}

function setAuthorizationBearer() {
  const accessToken = localStorage.getItem("access_token");
  if (accessToken) {
    axios.defaults.headers.common["Authorization"] = `Bearer ${accessToken}`;
  }
}


export default {
  //פונקציות של API :

  getLoginUser: () => {
    const accessToken = localStorage.getItem("access_token");
    if (accessToken) {
      return jwtDecode(accessToken);
    }
    return null;
  },

  logOut: () => {
    localStorage.setItem("access_token", "");
    delete axios.defaults.headers.common["Authorization"];

  },

 register: async (Username, Password) => {
    const res = await axios.post(`/register`, { Username, Password });
    saveAccessToken(res.data);
},

login: async (Username, Password) => {
    const res = await axios.post(`/login`, { Username, Password });
    saveAccessToken(res.data);
},

  getTasks: async () => {
    const result = await axios.get(`/items`)
    return result.data;
  },

  addTask: async (name) => {
    // שולחים אובייקט עם השם והמצב ההתחלתי (לא בוצע).
    const result = await axios.post(`/items`, { name, isComplete: false });
    return result.data;
  },

  setCompleted: async (id, isComplete) => {
    // שולחים את ה-ID ב-URL ואת הנתון המעודכן ב-Body.
    await axios.put(`/items/${id}`, { isComplete });
    return {};
  },


  deleteTask: async (id) => {
    await axios.delete(`/items/${id}`);
    return {};
  }
};
