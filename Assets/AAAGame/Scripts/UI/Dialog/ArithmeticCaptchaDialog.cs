using UnityEngine;
using UnityEngine.UI;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class ArithmeticCaptchaDialog : UIFormBase
{
    #region Private Fields
    [SerializeField] private RawImage m_CaptchaImage;
    [SerializeField] private InputField m_AnswerInput;
    [SerializeField] private Button m_VerifyButton;
    [SerializeField] private Button m_RefreshButton;
    
    private int m_CorrectAnswer;
    private bool m_IsVerified = false;
    private Texture2D m_CaptchaTexture;
    private string m_QuestionText = "";
    #endregion

    #region Unity Lifecycle
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        
        // 初始化按钮事件
        m_VerifyButton.onClick.AddListener(OnVerifyClicked);
        m_RefreshButton.onClick.AddListener(GenerateCaptcha);

        // 生成验证码
        GenerateCaptcha();
        
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 如果未验证，则回调失败
        if (!m_IsVerified)
        {
            Params.ButtonClickCallback?.Invoke(null, "No");
        }
        m_VerifyButton.onClick.RemoveAllListeners();
        m_RefreshButton.onClick.RemoveAllListeners();

        // 释放纹理资源
        if (m_CaptchaTexture != null)
        {
            Destroy(m_CaptchaTexture);
            m_CaptchaTexture = null;
        }
        base.OnClose(isShutdown, userData);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 生成算术验证码
    /// </summary>
    private void GenerateCaptcha()
    {
        // 生成算术问题
        int num1 = UnityEngine.Random.Range(1, 20);
        int num2 = UnityEngine.Random.Range(1, 20);
        int operationType = UnityEngine.Random.Range(0, 3);

        string operationSymbol = "";
        
        switch (operationType)
        {
            case 0: // 加法
                operationSymbol = "+";
                m_CorrectAnswer = num1 + num2;
                break;
            case 1: // 减法 (确保结果大于0)
                if (num1 < num2)
                {
                    int temp = num1;
                    num1 = num2;
                    num2 = temp;
                }
                if (num1 - num2 <= 0)
                {
                    num2 = UnityEngine.Random.Range(1, num1);
                }
                operationSymbol = "-";
                m_CorrectAnswer = num1 - num2;
                break;
            case 2: // 乘法 (限制数字大小防止结果过大)
                num1 = UnityEngine.Random.Range(1, 10);
                num2 = UnityEngine.Random.Range(1, 10);
                operationSymbol = "×";
                m_CorrectAnswer = num1 * num2;
                break;
        }

        m_QuestionText = $"{num1} {operationSymbol} {num2} = ?";
        m_AnswerInput.text = "";
        m_IsVerified = false;
        
        // 生成验证码图像
        GenerateCaptchaImage(m_QuestionText);
    }
    
    /// <summary>
    /// 生成验证码图像
    /// </summary>
    private void GenerateCaptchaImage(string question)
    {
        // 创建新纹理或重用现有纹理
        if (m_CaptchaTexture == null)
            m_CaptchaTexture = new Texture2D(256, 64, TextureFormat.ARGB32, false);
        
        // 填充背景色 (浅色背景)
        Color32 bgColor = new Color32(245, 245, 245, 255);
        Color32[] pixels = new Color32[m_CaptchaTexture.width * m_CaptchaTexture.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = bgColor;
        
        // 准备绘制
        m_CaptchaTexture.SetPixels32(pixels);
        
        // 绘制简单文本
        DrawSimpleText(question, 20, 30, 28);
        
        // 添加干扰元素
        AddSimpleNoise(100); // 添加适量噪点
        AddSimpleLines(4);   // 添加少量干扰线
        
        // 应用更改并更新UI
        m_CaptchaTexture.Apply();
        m_CaptchaImage.texture = m_CaptchaTexture;
    }

    /// <summary>
    /// 绘制简单清晰文本
    /// </summary>
    private void DrawSimpleText(string text, float x, float y, int fontSize)
    {
        float xPos = x;
        // 黑色字体
        Color textColor = Color.black;
        
        // 为每个字符绘制文本
        foreach (char c in text)
        {
            if (c == ' ')
            {
                xPos += fontSize * 0.5f;
                continue;
            }
            
            // 只有轻微偏移，不做旋转
            float yOffset = UnityEngine.Random.Range(-2f, 2f);
            
            // 字符位置
            int charX = Mathf.RoundToInt(xPos);
            int charY = Mathf.RoundToInt(y + yOffset);
            
            // 粗体效果
            DrawSimpleCharacter(c, charX, charY, fontSize, textColor);
            
            xPos += (c == '1') ? fontSize * 0.5f : fontSize * 0.7f; // 字符间距
        }
    }
    
    /// <summary>
    /// 绘制单个字符 (使用简单点阵模式)
    /// </summary>
    private void DrawSimpleCharacter(char c, int x, int y, int size, Color color)
    {
        string[,] patterns = null;
        
        // 简单的字符模式
        switch (c)
        {
            case '0':
                patterns = new string[5, 3] {
                    {" X "," X "," X "},
                    {"X  ","   ","  X"},
                    {"X  ","   ","  X"},
                    {"X  ","   ","  X"},
                    {" X "," X "," X "}
                };
                break;
            case '1':
                patterns = new string[5, 2] {
                    {" X", " X"},
                    {"XX", " X"},
                    {" X", " X"},
                    {" X", " X"},
                    {"XXX","XXX"}
                };
                break;
            case '2':
                patterns = new string[5, 3] {
                    {"XXX","XXX","XXX"},
                    {"   ","   ","  X"},
                    {"XXX","XXX","XXX"},
                    {"X  ","   ","   "},
                    {"XXX","XXX","XXX"}
                };
                break;
            case '3':
                patterns = new string[5, 3] {
                    {"XXX","XXX","XXX"},
                    {"   ","   ","  X"},
                    {"XXX","XXX","XXX"},
                    {"   ","   ","  X"},
                    {"XXX","XXX","XXX"}
                };
                break;
            case '4':
                patterns = new string[5, 3] {
                    {"X  ","   ","  X"},
                    {"X  ","   ","  X"},
                    {"XXX","XXX","XXX"},
                    {"   ","   ","  X"},
                    {"   ","   ","  X"}
                };
                break;
            case '5':
                patterns = new string[5, 3] {
                    {"XXX","XXX","XXX"},
                    {"X  ","   ","   "},
                    {"XXX","XXX","XXX"},
                    {"   ","   ","  X"},
                    {"XXX","XXX","XXX"}
                };
                break;
            case '6':
                patterns = new string[5, 3] {
                    {"XXX","XXX","XXX"},
                    {"X  ","   ","   "},
                    {"XXX","XXX","XXX"},
                    {"X  ","   ","  X"},
                    {"XXX","XXX","XXX"}
                };
                break;
            case '7':
                patterns = new string[5, 3] {
                    {"XXX","XXX","XXX"},
                    {"   ","   ","  X"},
                    {"   ","   ","  X"},
                    {"   ","   ","  X"},
                    {"   ","   ","  X"}
                };
                break;
            case '8':
                patterns = new string[5, 3] {
                    {"XXX","XXX","XXX"},
                    {"X  ","   ","  X"},
                    {"XXX","XXX","XXX"},
                    {"X  ","   ","  X"},
                    {"XXX","XXX","XXX"}
                };
                break;
            case '9':
                patterns = new string[5, 3] {
                    {"XXX","XXX","XXX"},
                    {"X  ","   ","  X"},
                    {"XXX","XXX","XXX"},
                    {"   ","   ","  X"},
                    {"XXX","XXX","XXX"}
                };
                break;
            case '+':
                patterns = new string[5, 3] {
                    {"   "," X ","   "},
                    {"   "," X ","   "},
                    {"XXX","XXX","XXX"},
                    {"   "," X ","   "},
                    {"   "," X ","   "}
                };
                break;
            case '-':
                patterns = new string[5, 3] {
                    {"   ","   ","   "},
                    {"   ","   ","   "},
                    {"XXX","XXX","XXX"},
                    {"   ","   ","   "},
                    {"   ","   ","   "}
                };
                break;
            case '×':
                patterns = new string[5, 3] {
                    {"X  ","   ","  X"},
                    {"   "," X ","   "},
                    {"   "," X ","   "},
                    {"   "," X ","   "},
                    {"X  ","   ","  X"}
                };
                break;
            case '=':
                patterns = new string[5, 3] {
                    {"   ","   ","   "},
                    {"XXX","XXX","XXX"},
                    {"   ","   ","   "},
                    {"XXX","XXX","XXX"},
                    {"   ","   ","   "}
                };
                break;
            case '?':
                patterns = new string[5, 3] {
                    {"XXX","XXX","XXX"},
                    {"   ","   ","  X"},
                    {"   ","XXX","XXX"},
                    {"   ","   ","   "},
                    {"   "," X ","   "}
                };
                break;
            default:
                return;
        }
        
        // 绘制字符
        if (patterns != null)
        {
            int patternHeight = patterns.GetLength(0);
            int patternWidth = patterns.GetLength(1);
            int scale = size / (patternHeight + 2); // 留出边距
            
            for (int i = 0; i < patternHeight; i++)
            {
                for (int j = 0; j < patternWidth; j++)
                {
                    if (patterns[i, j].Contains("X"))
                    {
                        // 粗体效果
                        for (int dx = 0; dx < scale; dx++)
                        {
                            for (int dy = 0; dy < scale; dy++)
                            {
                                int px = x + j * scale + dx;
                                int py = y - i * scale - dy;
                                
                                // 边界检查
                                if (px >= 0 && px < m_CaptchaTexture.width && 
                                    py >= 0 && py < m_CaptchaTexture.height)
                                {
                                    m_CaptchaTexture.SetPixel(px, py, color);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 添加简单噪点
    /// </summary>
    private void AddSimpleNoise(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int x = UnityEngine.Random.Range(0, m_CaptchaTexture.width);
            int y = UnityEngine.Random.Range(0, m_CaptchaTexture.height);
            
            // 检查是否是文字像素，避免干扰文字
            Color pixel = m_CaptchaTexture.GetPixel(x, y);
            if (pixel.r > 0.8f) // 如果是背景色
            {
                m_CaptchaTexture.SetPixel(x, y, new Color(0.3f, 0.3f, 0.3f, 0.8f));
            }
        }
    }

    /// <summary>
    /// 添加简单干扰线
    /// </summary>
    private void AddSimpleLines(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // 避开中央区域的线条
            int y1 = UnityEngine.Random.Range(0, m_CaptchaTexture.height);
            int y2 = UnityEngine.Random.Range(0, m_CaptchaTexture.height);
            
            int x1, x2;
            if (UnityEngine.Random.value > 0.5f)
            {
                // 水平干扰线
                x1 = 0;
                x2 = m_CaptchaTexture.width;
            }
            else
            {
                // 垂直干扰线
                x1 = UnityEngine.Random.Range(0, m_CaptchaTexture.width);
                x2 = UnityEngine.Random.Range(0, m_CaptchaTexture.width);
            }
            
            // 随机线色
            Color lineColor = new Color(
                UnityEngine.Random.Range(0.4f, 0.6f),
                UnityEngine.Random.Range(0.4f, 0.6f),
                UnityEngine.Random.Range(0.4f, 0.6f),
                0.5f
            );
            
            DrawLine(x1, y1, x2, y2, lineColor);
        }
    }

    /// <summary>
    /// 绘制线条
    /// </summary>
    private void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        
        while (true)
        {
            // 绘制当前点及其周围的点（细线效果）
            int px = x0;
            int py = y0;
            if (px >= 0 && px < m_CaptchaTexture.width && py >= 0 && py < m_CaptchaTexture.height)
            {
                m_CaptchaTexture.SetPixel(px, py, color);
            }
            
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    /// <summary>
    /// 验证答案
    /// </summary>
    private void OnVerifyClicked()
    {
        if (Util.IsClickLocked()) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);

        if (string.IsNullOrEmpty(m_AnswerInput.text))
        {
            GF.UI.ShowToast("请输入答案", 1.5f);
            return;
        }

        if (int.TryParse(m_AnswerInput.text, out int userAnswer))
        {
            if (userAnswer == m_CorrectAnswer)
            {
                m_IsVerified = true;
                Params.ButtonClickCallback?.Invoke(null, "Yes");
                // GF.UI.ShowToast("验证通过", 1.5f);
                CloseWithAnimation();
            }
            else
            {
                GF.UI.ShowToast("验证失败，请重新输入", 1.5f);
                GenerateCaptcha();
            }
        }
        else
        {
            GF.UI.ShowToast("请输入有效数字", 1.5f);
        }
    }

    #endregion

} 