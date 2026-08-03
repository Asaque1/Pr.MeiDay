using UnityEngine;
using UnityEngine.UI;

public class DayBar : MonoBehaviour
{
    public Image fillImage;

    private int currentDay = 0;
    private const int maxDay = 6;

    public void NextDay()
    {
        currentDay++;

        if (currentDay > maxDay)
            currentDay = 0;

        fillImage.fillAmount = (float)currentDay / maxDay;
    }
}