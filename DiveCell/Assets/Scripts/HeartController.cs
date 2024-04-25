using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HeartController : MonoBehaviour
{
    PlayerController player;

    private GameObject[] heartContainers;
    private Image[] heartFills;
    public Transform heartParents;
    public GameObject heartContainerPrefab;
    // Start is called before the first frame update
    void Start()
    {
        player = PlayerController.Instance;
        heartContainers = new GameObject[PlayerController.Instance.maxHealth];
        heartFills = new Image[PlayerController.Instance.maxHealth];

        PlayerController.Instance.onHealthChangedCallback += UpdateHeartHUD; // multicast the delegate 
        InstantiateHeartContainers();
        UpdateHeartHUD();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SetHeartContainers(){
        for(int i = 0; i < heartContainers.Length; i++){
            if(i < PlayerController.Instance.maxHealth){
                heartContainers[i].SetActive(true);
            }
            else{
                heartContainers[i].SetActive(false);
            }
        }
    }

    void SetFilledHearts(){
        for(int i = 0; i < heartFills.Length; i++){
            if(i < PlayerController.Instance.health){
                heartFills[i].fillAmount = 1;
            }
            else{
                heartFills[i].fillAmount = 0;
            }
        }
    }

    void InstantiateHeartContainers(){
        for(int i = 0; i < PlayerController.Instance.maxHealth; i++){
            GameObject temp = Instantiate(heartContainerPrefab);
            temp.transform.SetParent(heartParents, false);
            heartContainers[i] = temp;
            heartFills[i] = temp.transform.Find("HeartFill").GetComponent<Image>();
        }
    }

    void UpdateHeartHUD(){
        SetHeartContainers();
        SetFilledHearts();
    }

}
