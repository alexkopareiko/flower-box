using UnityEngine;

namespace Game
{
    public class UISubCanvas : MonoBehaviour
    {
        protected void Show() 
        {
            gameObject.SetActive(true);
        }
        protected void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
