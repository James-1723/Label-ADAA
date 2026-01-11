using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class EmotionFortunePromptComposer : MonoBehaviour
{
    public ComfyPromptCtr comfyPromptCtr; // ��������̪��P�@��

    // �̧A���D�N���� API�G�α���/�Ҹ� �� �� prompt �� �e�X �� �^���ɦW
    public async Task<string> ComposeAndSpawn(string emotion, string fortune)
    {
        string pos = BuildPositive(emotion, fortune);
        string neg = BuildNegative(emotion, fortune);
        string fileName = await comfyPromptCtr.QueuePromptAsync(pos, neg);
        return fileName; // "xxx.png"
    }

    private string BuildPositive(string emotion, string fortune)
    {
        // �i��y����������ҪO/�v��
        var sb = new StringBuilder();
        sb.Append("Generate an antique style and romanticism style, ");
        sb.Append($"in the background of a story describes '{fortune}', ");
        sb.Append($"combining colors matching the {emotion} emotions ");
        sb.Append("with 1 or 2 burned spots fit the story.");
        return sb.ToString();
    }

    private string BuildNegative(string emotion, string fortune)
    {
        // �i�����p�ʺA�վ�
        var sb = new StringBuilder();
        sb.Append("lowres, bad anatomy, blurry, artifacts, watermark, jpeg artifacts, ");
        sb.Append("text, logo, extra limbs, mutated, nsfw, low contrast");
        return sb.ToString();
    }
}