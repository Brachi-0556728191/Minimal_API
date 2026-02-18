using System;
using System.Collections.Generic;

namespace TodoApi;
public class User
{
    public int Id { get; set; } // מזהה ייחודי (Primary Key)
    public string Username { get; set; } // שם המשתמש לכניסה
    public string Password { get; set; } // סיסמה (בפרויקט לימודי נשמור טקסט פשוט, בעולם האמיתי מצפינים!)
}