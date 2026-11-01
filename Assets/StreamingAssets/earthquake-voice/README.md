# 地震紧急播报语音

## 放在哪

```
StreamingAssets/earthquake-voice/ja/earthquake_alert.wav   ← 日语
StreamingAssets/earthquake-voice/zh/earthquake_alert.wav   ← 中文
StreamingAssets/earthquake-voice/en/earthquake_alert.wav   ← 英语
```

文件名对应 `EarthquakeController.announcementKey`（默认 `earthquake_alert`），
语言目录跟随 `LanguageManager` 的当前语言，也可以用
`announcementLanguageOverride` 强制指定。

**格式必须是 WAV** —— 加载走 `UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV)`。
用 UnityWebRequest 而不是 `System.IO` 是因为 Android 上 StreamingAssets 打包在 APK 里，
路径形如 `jar:file:///...`，File API 读不了。这和 AED 语音是同一套做法。

**没有文件也能跑**：找不到时会退回代码合成的双音警报，Console 会提示一行。
放进文件后自动改用它，不用改代码。

---

## 为什么没有内置真实的緊急地震速報音

两个原因，都不是技术问题：

**著作权**。緊急地震速報のチャイム音（NHK 版本的那段旋律）由伊勢正義作曲，
著作权归属明确，商用或再分发需要授权。

**社会规范**。日本社会对这个音在非紧急场合的播放有明确抵触 ——
人们被训练成听到就立即避难，随意播放会削弱它的效力，也可能造成真实惊吓。
防災訓練用途通常使用「速報風」的替代音，而不是原音。

所以这里合成的占位音刻意用了与原音不同的音程关系，只作为「有声音」的兜底。

---

## 可用的素材来源

以下都允许商用、且不要求逐次报备（**使用前请自行复核当下的利用规约**，
条款会变，而且你这是要交付的教学项目）：

| 来源 | 说明 |
|---|---|
| [効果音ラボ](https://soundeffect-lab.info/) | 商用免费、无需署名、无需报备。有警报类音效 |
| [OtoLogic](https://otologic.jp/free/se/news-alert01.html) | ニュース速報・警報風 音效，署名方式见站内规约 |
| [ポケットサウンド](https://pocket-se.info/archives/1247/) | 有「緊急地震速報（オリジナル）」—— 原创版，不是 NHK 原音 |

**「オリジナル」这个标注很重要**：它表示是模仿风格的原创音，
不是受著作权保护的原音，这正是防災訓練该用的东西。

---

## 更推荐：自己合成播报语音

用 [VOICEVOX](https://voicevox.hiroshiba.jp/)（免费、本地运行的日语语音合成）
生成你想要的播报词。这样：

- 内容完全可控，可以针对你的训练场景定制
- 三种语言各生成一版，风格统一
- 商用免费，**但必须标注credits**（形如 `VOICEVOX:ずんだもん`），
  且不同角色的版元可能有各自附加条款，用前逐个确认

导出时选 WAV，直接放进对应语言目录即可。

### 播报词参考

按日本防災訓練的常见表述，可以这样写（**内容请由防災或医学教员核定**，
我不对表述的规范性负责）：

**ja**
```
緊急地震速報。強い揺れに警戒してください。
身の安全を確保してください。頭を守り、その場で待機してください。
```

**zh**
```
紧急地震速报。请警惕强烈震动。
请确保自身安全，护住头部，原地待命。
```

**en**
```
Earthquake early warning. Strong shaking expected.
Protect your head and take cover where you are.
```

---

## 相关 Inspector 参数

`EarthquakeController` 上：

| 参数 | 说明 |
|---|---|
| `playEmergencyAnnouncement` | 总开关，默认开 |
| `announcementKey` | 文件名（不含扩展名），默认 `earthquake_alert` |
| `announcementLanguageOverride` | 留空 = 跟随 LanguageManager |
| `announcementSource` | 留空会自动创建（2D、不做空间定位，priority=0 压过轰鸣声） |
