using UnityEngine;

public class InstructionManager : MonoBehaviour
{
    public GameObject instructionImage; // The image that will show the instructions

    public void ShowInstructions()
    {
        instructionImage.SetActive(true); // Show the image when the button is clicked
    }

    public void HideInstructions()
    {
        instructionImage.SetActive(false); // Hide the image (you can add this to a close button)
    }
}
