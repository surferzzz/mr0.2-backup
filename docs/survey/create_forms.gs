/**
 * VR-CPR 研究：体験後アンケートを作る Apps Script。
 *
 * 使い方
 *   1. https://script.google.com で新しいプロジェクトを作る
 *   2. このファイルの中身をぜんぶ貼り付けて保存
 *   3. 関数 createSurvey を選んで実行（初回は認可を求められる）
 *   4. 実行ログに出る URL を控える
 *   5. 2026-11-02 に作った古いフォーム 3 本は retireOldForms で止める
 *   6. 実験者が毎回記入する記録フォームは createExperimenterForm で作る
 *      （回答は体験後アンケートと同じスプレッドシートの別タブに入る）
 *
 * 設計（questionnaire_visual_vs_haptic.md と対応）
 *   - 被験者間：1 人は A（画面だけ）か B（画面＋手の振動）の一方だけを体験する
 *   - 参加者 50 人（各群 25 人）
 *   - 質問紙は体験後の 1 本だけ。事前アンケートは廃止したので、
 *     自己効力感は「体験後の水準」を群間で比べる。変化量は出せない。
 *   - 尺度はすべて 0〜10（NASA-TLX にそろえた）
 *
 * 項目コード（SE1, SUS3 …）は質問文の先頭に埋め込んである。これが無いと
 * スプレッドシートの列名が質問文そのままになり、集計時にどの列か分からなくなる。
 */

var SHEET_NAME = 'VR-CPR 研究 回答データ';

// 全部の尺度を NASA-TLX にそろえて 0〜10 の評価にしてある。
// 答える側の手が一つで済み、集計も列をまたいで素直に比べられる。
// 代償：SUS の 0〜100 換算式は 1〜5 の 5 件法が前提なので、そのままでは使えない
//      （換算のしかたは questionnaire_visual_vs_haptic.md を見ること）。
var RATING_COLS = range_(0, 10);

var AGREE_HELP = '0 = まったくそう思わない　／　10 = とてもそう思う';
var SSQ_HELP = '0 = ぜんぜんない　／　10 = とても強い';

// ───────────────────────────────── entry point

function createSurvey() {
  var ss = SpreadsheetApp.create(SHEET_NAME);
  var form = buildPostForm_(ss.getId());

  var out = [
    '',
    '===== できあがり =====',
    '回答データ（スプレッドシート）: ' + ss.getUrl(),
    '',
    '体験後アンケート（1 人 1 回）',
    '    回答用: ' + form.getPublishedUrl(),
    '    編集用: ' + form.getEditUrl(),
    '',
    '※ 本番の前に、自分でダミー回答を入れてシートの列を必ず確認すること。',
    '※ 古いフォーム 3 本は retireOldForms で止める。'
  ].join('\n');

  Logger.log(out);
  return out;
}

// ───────────────────────────────── 体験後アンケート（これ 1 本）

function buildPostForm_(spreadsheetId) {
  var form = FormApp.create('【VR-CPR 研究】体験後アンケート');
  applyCommonSettings_(form);

  form.setDescription(
    'この研究について\n\n' +
    'VR（ゴーグル）を使った心肺蘇生（CPR）の練習で、\n' +
    '手に振動があると学びやすくなるかどうかを調べています。\n\n' +
    '・このアンケートは体験のあとに 1 回だけお答えいただきます。7 分くらいです。\n' +
    '・答えは匿名です。名前や学籍番号は聞きません。\n' +
    '・答えた内容が成績に関係することはありません。\n' +
    '・答えたくない質問は飛ばしてかまいません。\n' +
    '・いつでも参加をやめられます。やめても不利になることはありません。\n\n' +
    'わからないことがあれば、担当者に聞いてください。'
  );

  // ── P1 同意（ページ区切りの前＝1 ページ目） ──
  var consent = form.addMultipleChoiceItem()
    .setTitle('CONSENT. 説明を読みました。この研究に参加します。')
    .setRequired(true);

  // ── P2 あなたについて ──
  var secAbout = form.addPageBreakItem().setTitle('あなたについて');
  addItem_(form, 'P1. 参加者ID（担当者から聞いた番号）', 'text', { required: true });
  // 被験者間なので、これは「今回どちらか」ではなく割り付けられた群の記録。
  addItem_(form, 'P2. 体験したやり方', 'radio', {
    required: true,
    choices: ['A：画面の表示だけ（振動なし）', 'B：画面の表示＋手の振動']
  });
  addItem_(form, 'D2. 年齢', 'text', {});
  addItem_(form, 'D3. 性別', 'radio', { choices: ['男性', '女性', 'その他', '答えない'] });
  addItem_(form, 'D4. 所属', 'radio', { choices: ['学部', '大学院', 'その他'] });
  addItem_(form, 'D6. メガネ・コンタクトを使っていますか', 'radio', {
    choices: ['使っていない', 'メガネ', 'コンタクト']
  });
  addItem_(form, 'V1. VR のゴーグルを使ったことがありますか', 'radio', {
    choices: ['今回が初めて', '1〜2 回', '3〜10 回', '10 回以上・よく使う']
  });
  addItem_(form, 'V2. 振動する機器（触覚デバイス）を使ったことがありますか', 'radio', {
    choices: ['ない', 'ゲームのコントローラくらい', '専用の機器を使ったことがある']
  });
  addItem_(form, 'V4. 乗り物に酔いやすいですか', 'radio', {
    choices: ['まったく酔わない', 'あまり酔わない', '少し酔う', 'よく酔う']
  });

  // ── P3 CPR の経験（分岐の要） ──
  form.addPageBreakItem().setTitle('CPR の経験');
  var expGate = form.addMultipleChoiceItem()
    .setTitle('EXP0. CPR（心肺蘇生）の講習を受けたことがありますか')
    .setRequired(true);

  // ── P4 CPR の経験（くわしく） ──
  var secExpDetail = form.addPageBreakItem().setTitle('CPR の経験（くわしく）');
  addItem_(form, 'EXP1. どんな講習を受けましたか（あてはまるものすべて）', 'checkbox', {
    choices: ['BLS（AHA/JRC）', 'ICLS', 'ACLS', '普通救命講習（消防）', '大学の授業・実習',
              '部活・サークル', '運転免許の講習', 'その他']
  });
  addItem_(form, 'EXP2. 最後に受けたのはいつですか', 'radio', {
    choices: ['6 か月以内', '6 か月〜1 年', '1〜3 年', '3 年より前']
  });
  addItem_(form, 'EXP3. 今までに何回受けましたか', 'radio', {
    choices: ['1 回', '2〜3 回', '4〜5 回', '6 回以上']
  });
  addItem_(form, 'EXP4. 人形（マネキン）で胸を押す練習をしたことがありますか', 'radio',
    { choices: ['ある', 'ない'] });
  addItem_(form, 'EXP5. 押す深さや速さが表示される人形を使ったことがありますか', 'radio',
    { choices: ['ある', 'ない', 'わからない'] });
  addItem_(form, 'EXP6. 本当の場面で CPR をしたことがありますか', 'radio',
    { choices: ['ない', '1 回', '2 回以上'] });
  addItem_(form, 'EXP7. AED を使ったことがありますか（練習用もふくみます）', 'radio',
    { choices: ['ある', 'ない'] });

  // ── P5 使いやすさ（SUS） ──
  var secSus = form.addPageBreakItem().setTitle('使いやすさ');
  likertGrid_(form, 'このシステムについて答えてください', AGREE_HELP,
    [
      'SUS1. このシステムをまた使いたいと思う',
      'SUS2. このシステムは、必要以上に複雑だと思った',
      'SUS3. このシステムは簡単に使えた',
      'SUS4. 使うのに、人に助けてもらう必要があると思った',
      'SUS5. いろいろな機能が、うまくまとまっていると思った',
      'SUS6. ちぐはぐな（一貫していない）ところが多いと思った',
      'SUS7. たいていの人は、すぐに使い方を覚えられると思う',
      'SUS8. このシステムはとても使いにくいと思った',
      'SUS9. 安心して操作できた',
      'SUS10. 使い始める前に、覚えることが多かった'
    ],
    RATING_COLS);

  // ── P6 大変さ（NASA-TLX） ──
  form.addPageBreakItem().setTitle('大変さ');
  likertGrid_(form, '今回の練習の大変さについて答えてください',
    '0 = ぜんぜん　／　10 = とても',
    [
      'TLX1. 頭の負担：どれくらい考えたり、注意したりしましたか',
      'TLX2. 体の負担：どれくらい体がきつかったですか',
      'TLX3. 急がされた感じ：どれくらい焦りましたか',
      'TLX4. できばえ：0 = 完ぺきにできた／10 = ぜんぜんできなかった',
      'TLX5. がんばり：どれくらいがんばりましたか',
      'TLX6. いらだち：どれくらいイライラ・不安を感じましたか'
    ],
    RATING_COLS);

  // ── P7 リアルさ ──
  form.addPageBreakItem().setTitle('リアルさ・手の感覚');
  likertGrid_(form, '今回の練習について答えてください', AGREE_HELP,
    [
      'PR1. 胸を押したときの手ごたえは、本物の人に近いと感じた',
      'PR2. 押す深さが足りているか、自分でわかった',
      'PR3. ちょうどよい速さを保ちやすかった',
      'PR4. 本当に救命をしているように感じた',
      'PR5. 深さや速さがずれたとき、すぐに気づけた',
      'PR6. 画面に出る情報（深さ・速さ）はわかりやすかった',
      'PR7. 練習に集中できて、まわりが気にならなかった',
      'PR8. AED の使い方の流れはわかりやすかった'
    ],
    RATING_COLS);

  // ── P8 手の振動（B 群だけ・任意） ──
  // 分岐ではなく「任意回答」にしてある。P2 で分岐させると SUS・TLX・リアルさまで
  // A 用と B 用に二重化する羽目になり、つけ間違えると回答が丸ごと飛ぶ。
  // 空欄なら集計で落とせばいいだけ。
  form.addPageBreakItem()
    .setTitle('手の振動について')
    .setHelpText(
      '※ ここは「画面の表示＋手の振動」を体験した人だけ答えてください。\n' +
      '　 振動なしだった人は、答えずに次へ進んでください。'
    );
  likertGrid_(form, '手の振動について答えてください', AGREE_HELP,
    [
      'HP1. 手の振動は、押す動きとタイミングが合っていた',
      'HP2. 振動で「十分な深さまで押せた」ことがわかった',
      'HP3. 振動で「浅すぎる」「深すぎる」の違いがわかった',
      'HP4. 振動があると、速さを保ちやすかった',
      'HP5. AED の電気ショックのときの振動で、状況がわかりやすくなった',
      'HP6. 振動があると、本物らしく感じた',
      'HP7. 振動が気になって、かえって集中できなかった',
      'HP8. 次も振動のあるほうで練習したい'
    ],
    RATING_COLS, false);

  // 双極尺度。5 が「ちょうどよい」なので、他のグリッドに混ぜると平均が無意味になる。
  // 分析では |HP9 - 5| を「ちょうどよさからのズレ」として扱う。
  form.addScaleItem()
    .setTitle('HP9. 振動の強さはどうでしたか（5 = ちょうどよい）')
    .setBounds(0, 10)
    .setLabels('弱すぎる', '強すぎる')
    .setRequired(false);

  // ── P9 今の自信 ──
  // 事前アンケートを廃止したので、これは「体験後の水準」。変化量は出せない。
  // 無作為割付なので、群間の比較としては成立する。
  form.addPageBreakItem().setTitle('今の自信');
  likertGrid_(form, '今のお気持ちについて答えてください', AGREE_HELP,
    [
      'SE1. 人が倒れていたら、落ち着いて対応できる',
      'SE2. 胸を押す正しい位置がわかる',
      'SE3. ちょうどよい深さ（5 cm くらい）で押せる',
      'SE4. ちょうどよい速さ（1 分間に 100〜120 回）を保てる',
      'SE5. AED を正しく使える',
      'SE6. 本当の場面で、行動する自信がある'
    ],
    RATING_COLS);

  // ── P10 今の体調 ──
  form.addPageBreakItem().setTitle('今の体調');
  likertGrid_(form, '今の体調について答えてください', SSQ_HELP,
    [
      'SSQ1. 気分が悪い',
      'SSQ2. 目が疲れている',
      'SSQ3. 頭が痛い',
      'SSQ4. めまいがする',
      'SSQ5. 吐き気がする'
    ],
    RATING_COLS);

  // ── P11 自由記述 ──
  form.addPageBreakItem().setTitle('自由に書いてください');
  addItem_(form, 'F1. よかったところ、役に立ったところを書いてください', 'paragraph', {});
  addItem_(form, 'F2. 使いにくかったところ、直してほしいところを書いてください', 'paragraph', {});
  addItem_(form, 'F3. おかしいと思ったところ（不具合）があれば書いてください', 'paragraph', {});

  // ── 分岐を結ぶ（ページを全部作ってから） ──
  consent.setChoices([
    consent.createChoice('はい', secAbout),
    consent.createChoice('いいえ', FormApp.PageNavigationType.SUBMIT)
  ]);
  expGate.setChoices([
    expGate.createChoice('はい', secExpDetail),
    expGate.createChoice('いいえ', secSus)      // 経験なしは「くわしく」を飛ばす
  ]);
  secExpDetail.setGoToPage(secSus);             // 答え終わったら合流

  form.setDestination(FormApp.DestinationType.SPREADSHEET, spreadsheetId);
  return form;
}

// ───────────────────────────────── 実験者記録（実験者が毎回記入する）

// 体験後アンケートの回答先と同じスプレッドシート。別タブとして入るので、
// 参加者ID で被試の回答と突き合わせられる。
var SURVEY_SHEET_ID = '1vlpQJj3jBcWBIPadSlJbLdicosjm6ghC1tWmoheXZNU';

/**
 * 実験 1 回ごとに実験者が記入する記録フォームを作る。被試には見せない。
 *
 * 学外の人が触る必要はないので、組織内限定（ログイン必須）はあえて外さない。
 * 学外の実験者が記入するなら、作成後に設定画面で外すこと。
 */
function createExperimenterForm() {
  var form = FormApp.create('【VR-CPR 研究】実験者記録');
  form.setDescription(
    '実験 1 回ごとに、実験者が記入する記録です。被試には見せないでください。\n' +
    '参加者ID は体験後アンケートと同じ番号を使います。'
  );
  form.setCollectEmail(false);
  form.setAllowResponseEdits(false);
  form.setProgressBar(false);
  form.setShuffleQuestions(false);

  addItem_(form, 'E1. 参加者ID', 'text', { required: true });
  addItem_(form, 'E2. 群', 'radio', {
    required: true, choices: ['A：画面の表示だけ', 'B：画面の表示＋手の振動']
  });
  addItem_(form, 'E3. CPR 経験（割付の層）', 'radio', { required: true, choices: ['あり', 'なし'] });
  form.addDateItem().setTitle('E4. 実施日').setRequired(true);
  form.addTimeItem().setTitle('E5. 開始時刻');
  addItem_(form, 'E6. 実験者名', 'text', { required: true });

  // ID や群の設定し忘れは、あとから見分ける方法がない。毎回ここで確認させる。
  addItem_(form, 'E7. Unity 側の参加者ID と群（session_config.json）は割付表どおりでしたか', 'radio', {
    required: true, choices: ['はい', 'いいえ（その場で直した）', 'いいえ（直さずに実施した）']
  });

  addItem_(form, 'E8. 機材の不具合', 'radio', { required: true, choices: ['なし', 'あり'] });
  // B 群でグローブが止まっていたら、その回は「触覚あり」として扱えない。
  addItem_(form, 'E9. B 群の場合：グローブの振動は最後まで動いていましたか', 'radio', {
    required: true, choices: ['はい', 'いいえ（途中で止まった）', 'A 群なので該当なし']
  });
  addItem_(form, 'E10. 体験の完了', 'radio', { required: true, choices: ['最後まで完了', '途中で中断'] });

  // 倫理審査上、体験中・直後の不調は有害事象として記録しておく。
  addItem_(form, 'E11. 体験中・直後の不調（あてはまるものすべて）', 'checkbox', {
    required: true,
    choices: ['なし', '気分不良', 'めまい', '吐き気', '頭痛', '目の疲れ', '転倒・物への接触', 'その他']
  });

  addItem_(form, 'E12. 体験後アンケート', 'radio', { required: true, choices: ['回答済み', '未回答'] });
  addItem_(form, 'E13. CSV（sessions.csv / compressions.csv）', 'radio', {
    required: true, choices: ['回収済み', '未回収']
  });

  addItem_(form, 'E14. 不具合・中断・不調の内容と対応', 'paragraph', {
    help: 'E8〜E11 で「あり」「いいえ」「中断」「なし以外」を選んだときは必ず書く'
  });
  addItem_(form, 'E15. 備考', 'paragraph', {});

  form.setDestination(FormApp.DestinationType.SPREADSHEET, SURVEY_SHEET_ID);

  var out = [
    '',
    '===== 実験者記録フォーム =====',
    '記入用: ' + form.getPublishedUrl(),
    '編集用: ' + form.getEditUrl(),
    '回答先: https://docs.google.com/spreadsheets/d/' + SURVEY_SHEET_ID + '/edit （新しいタブ）'
  ].join('\n');

  Logger.log(out);
  return out;
}

// ───────────────────────────────── 古いフォームを止める

// 2026-11-02 に作った 3 本（ID は編集用 URL の /d/ と /edit の間）
var OLD_PRE_FORM_ID = '1FLUkhoAYiycuJbPG2Mefjfm6QdNUWAQLRVXmxmXSXmo';
var OLD_POST_FORM_ID = '19bcDQxGRkwu7HbYy3Vvx2RNuZlf_Lkyb_aDqr7dEhBY';
var OLD_COMPARE_FORM_ID = '1X72YP5FzarXnH_kSDWqSjTEa_btRo582f8yRVPkQ4q0';

/**
 * 古い 3 本の回答受付を止めて、タイトルに【使用しない】を付ける。
 * 削除はしない——取り返しがつかないので、捨てるかどうかは人間が決める。
 */
function retireOldForms() {
  var ids = [OLD_PRE_FORM_ID, OLD_POST_FORM_ID, OLD_COMPARE_FORM_ID];
  var log = [];

  for (var i = 0; i < ids.length; i++) {
    try {
      var form = FormApp.openById(ids[i]);
      form.setAcceptingResponses(false);
      if (form.getTitle().indexOf('【使用しない】') !== 0) {
        form.setTitle('【使用しない】' + form.getTitle());
      }
      log.push('止めた: ' + form.getTitle() + ' / ' + form.getEditUrl());
    } catch (e) {
      log.push('開けなかった（すでに削除済み？）: ' + ids[i] + ' — ' + e.message);
    }
  }

  log.push('\n不要なら Drive で手で削除すること。');
  Logger.log(log.join('\n'));
}

// ───────────────────────────────── 共通処理

function applyCommonSettings_(form) {
  form.setCollectEmail(false);
  form.setAllowResponseEdits(false);
  form.setProgressBar(true);
  form.setShuffleQuestions(false);
  form.setLimitOneResponsePerUser(false);

  // 組織内限定を外す。大学アカウントだと既定で ON になっていて、
  // これを外し忘れると学外の参加者が送信できない。
  // 管理者がログイン必須を強制している環境では例外になるので、その場合は手で外す。
  try {
    form.setRequireLogin(false);
  } catch (e) {
    Logger.log('【要確認】setRequireLogin に失敗: ' + e.message +
      '\n  → フォームの設定で「〇〇大学 と信頼できる組織のユーザーに限定する」を手で外してください: ' +
      form.getEditUrl());
  }
}

/**
 * リッカート尺度はグリッドで作る。1 項目ずつラジオにすると
 * 回答者が十数画面スクロールすることになり、適当に答えられる率が上がる。
 */
function likertGrid_(form, title, help, rows, columns, required) {
  var item = form.addGridItem()
    .setTitle(title)
    .setRows(rows)
    .setColumns(columns);
  if (help) item.setHelpText(help);
  item.setRequired(required === false ? false : true);
  return item;
}

function addItem_(form, title, type, opt) {
  opt = opt || {};
  var item;

  if (type === 'text') {
    item = form.addTextItem();
  } else if (type === 'paragraph') {
    item = form.addParagraphTextItem();
  } else if (type === 'radio') {
    item = form.addMultipleChoiceItem();
  } else if (type === 'checkbox') {
    item = form.addCheckboxItem();
  } else {
    throw new Error('unknown item type: ' + type);
  }

  item.setTitle(title);
  if (opt.choices) item.setChoiceValues(opt.choices);
  if (opt.help) item.setHelpText(opt.help);
  item.setRequired(opt.required === true);
  return item;
}

function range_(a, b) {
  var r = [];
  for (var i = a; i <= b; i++) r.push(String(i));
  return r;
}
