using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Cal {//41 +-0.9 F
    public static double TempC(string temp) {
        double value;
        if (double.TryParse(temp, out value))
        {
            return (value / 65536 * 175) - 45;
        }
        else
        {
            // หากเกิดข้อผิดพลาดในการแปลงเป็น double
            // ให้คืบควบคุมหรือรายงานข้อผิดพลาดตามที่คุณต้องการ
            // ในกรณีนี้จะให้ค่าเริ่มต้นเป็น 0 หรืออื่นๆ ตามความเหมาะสม
            return 0;
        }
    }

    public static double TempF(string temp) {
        double valueC;
        if (double.TryParse(temp, out valueC))
        {
            valueC = (valueC / 65536 * 175) - 45;
            return CelsiusToFahrenheit(valueC);
        }
        else
        {
            // หากเกิดข้อผิดพลาดในการแปลงเป็น double
            // ให้คืบควบคุมหรือรายงานข้อผิดพลาดตามที่คุณต้องการ
            // ในกรณีนี้จะให้ค่าเริ่มต้นเป็น 0 หรืออื่นๆ ตามความเหมาะสม
            return 0;
        }
    }

    public static double CelsiusToFahrenheit(double celsius) {
        return (celsius * 9 / 5) + 32;
    }
}
