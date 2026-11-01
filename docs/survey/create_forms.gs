/**
 * VR-CPR 研究：Google フォームを 3 本まとめて作る Apps Script。
 *
 * 使い方
 *   1. https://script.google.com で新しいプロジェクトを作る
 *   2. このファイルの中身をぜんぶ貼り付けて保存
 *   3. 関数 createAllForms を選んで実行（初回は認可を求められる）
 *   4. 実行ログに出る 4 本の URL を控える
 *
 * 作られるもの
 *   - スプレッドシート 1 冊（3 本ぶんの回答が別タブに入る）
 *   - 事前アンケート（CPR 経験で分岐）
 *   - 体験後アンケート（1 人 1 回）
 *
 * 設計の根拠は questionnaire_visual_vs_haptic.md を見ること。
 * 項目コード（SE1, SUS3 …）は質問文の先頭に埋め込んである。これが無いと
 * スプレッドシートの列名が質問文そのままになり、集計時にどの列か分からなくなる。
 */

var SHEET_NAME = 'VR-CPR 研究 回答データ';

// 事前と事後で一字一句そろえる必要がある（対応のある比較に使うため）。
// 片方だけ直すと列が別物になって突き合わせられなくなるので、ここで 1 回だけ定義する。
var SELF_EFFICACY_ROWS = [
  'SE1. 人が倒れていたら、落ち着いて対応できる',
  'SE2. 胸を押す正しい位置がわかる',
  'SE3. ちょうどよい深さ（5 cm くらい）で押せる',
  'SE4. ちょうどよい速さ（1 分間に 100〜120 回）を保てる',
  'SE5. AED を正しく使える',
  'SE6. 本当の場面で、行動する自信がある'
];

var SSQ_ROWS = [
  'SSQ1. 気分が悪い',
  'SSQ2. 目が疲れている',
  'SSQ3. 頭が痛い',
  'SSQ4. めまいがする',
  'SSQ5. 吐き気がする'
];

// 全部の尺度を NASA-TLX にそろえて 0〜10 の評価にしてある。
// 答える側の手が一つで済み、集計も列をまたいで素直に比べられる。
// 代償：SUS の 0〜100 換算式は 1〜5 の 5 件法が前提なので、そのままでは使えない
//      （換算のしかたは questionnaire_visual_vs_haptic.md を見ること）。
var RATING_COLS = range_(0, 10);

var AGREE_HELP = '0 = まったくそう思わない　／　10 = とてもそう思う';
var SSQ_HELP = '0 = ぜんぜんない　／　10 = とても強い';

// ───────────────────────────────── entry point

function createAllForms() {
  var ss = SpreadsheetApp.create(SHEET_NAME);

  var pre = createPreForm_(ss.getId());
  var post = createPostForm_(ss.getId());

  var out = [
    '',
    '===== できあがり =====',
    '回答データ（スプレッドシート）: ' + ss.getUrl(),
    '',
    '[1] 事前アンケート',
    '    回答用: ' + pre.getPublishedUrl(),
    '    編集用: ' + pre.getEditUrl(),
    '[2] 体験後アンケート（1 人 2 回）',
    '    回答用: ' + post.getPublishedUrl(),
    '    編集用: ' + post.getEditUrl(),
    '',
    '※ 本番の前に、自分で 2 周ぶんダミー回答してシートの列を必ず確認すること。'
  ].join('\n');

  Logger.log(out);
  return out;
}

// ───────────────────────────────── 1. 事前アンケート

function createPreForm_(spreadsheetId) {
  var form = FormApp.create('【VR-CPR 研究】事前アンケート');
  applyCommonSettings_(form);

  form.setDescription(
    'この研究について\n\n' +
    'VR（ゴーグル）を使った心肺蘇生（CPR）の練習で、\n' +
    '手に振動があると学びやすくなるかどうかを調べています。\n\n' +
    '・時間：体験とあわせて約45分です。アンケートは1回5分くらいです。\n' +
    '・答えは匿名です。名前や学籍番号は聞きません。\n' +
    '・答えた内容が成績に関係することはありません。\n' +
    '・気分が悪くなったら、いつでもやめられます。\n' +
    '・途中でやめても、不利になることはありません。\n\n' +
    'わからないことがあれば、担当者に聞いてください。'
  );

  // 同意は 1 ページ目（ページ区切りの前）に置く
  var consent = form.addMultipleChoiceItem()
    .setTitle('CONSENT. 説明を読みました。この研究に参加します。')
    .setRequired(true);

  // 先にページを全部作ってから分岐を結ぶ
  var secBasic = form.addPageBreakItem().setTitle('あなたについて');
  addItem_(form, 'D1. 参加者ID（担当者から聞いた番号）', 'text', { required: true });
  addItem_(form, 'D2. 年齢', 'text', {});
  addItem_(form, 'D3. 性別', 'radio', { choices: ['男性', '女性', 'その他', '答えない'] });
  addItem_(form, 'D4. 所属', 'radio', { choices: ['学部', '大学院', 'その他'] });
  addItem_(form, 'D6. メガネ・コンタクトを使っていますか', 'radio', {
    choices: ['使っていない', 'メガネ', 'コンタクト']
  });

  var secExpQ = form.addPageBreakItem().setTitle('CPR の経験');
  var expGate = form.addMultipleChoiceItem()
    .setTitle('EXP0. CPR（心肺蘇生）の講習を受けたことがありますか')
    .setRequired(true);

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

  var secVr = form.addPageBreakItem().setTitle('VR・機器の経験');
  addItem_(form, 'V1. VR のゴーグルを使ったことがありますか', 'radio', {
    choices: ['今回が初めて', '1〜2 回', '3〜10 回', '10 回以上・よく使う']
  });
  addItem_(form, 'V2. 振動する機器（触覚デバイス）を使ったことがありますか', 'radio', {
    choices: ['ない', 'ゲームのコントローラくらい', '専用の機器を使ったことがある']
  });
  // V3（ゲーム頻度）は削除した。欠番は埋めない——番号を詰めると
  // 既に書いた集計スクリプトや文書との対応が崩れるため。
  addItem_(form, 'V4. 乗り物に酔いやすいですか', 'radio', {
    choices: ['まったく酔わない', 'あまり酔わない', '少し酔う', 'よく酔う']
  });

  form.addPageBreakItem().setTitle('今の自信');
  likertGrid_(form, '今のお気持ちについて答えてください', AGREE_HELP, SELF_EFFICACY_ROWS, RATING_COLS);

  form.addPageBreakItem().setTitle('今の体調');
  likertGrid_(form, '今の体調について答えてください',
    SSQ_HELP + '（体験の前の状態を記録しておくためのものです）',
    SSQ_ROWS, RATING_COLS);

  // 分岐を結ぶ
  consent.setChoices([
    consent.createChoice('はい', secBasic),
    consent.createChoice('いいえ', FormApp.PageNavigationType.SUBMIT)
  ]);
  expGate.setChoices([
    expGate.createChoice('はい', secExpDetail),
    expGate.createChoice('いいえ', secVr)
  ]);
  // 「くわしく」を答え終わったら VR のページへ（経験なしの人と合流する）
  secExpDetail.setGoToPage(secVr);

  form.setDestination(FormApp.DestinationType.SPREADSHEET, spreadsheetId);
  return form;
}

// ───────────────────────────────── 2. 体験後アンケート

function createPostForm_(spreadsheetId) {
  var form = FormApp.create('【VR-CPR 研究】体験後アンケート');
  applyCommonSettings_(form);

  form.setDescription('いま体験した練習について答えてください。');

  addItem_(form, 'P1. 参加者ID', 'text', { required: true });
  // 被験者間計画なので、P2 は「今回どちらだったか」ではなく群の記録。
  // 参加者はどちらか一方しか体験しない。
  addItem_(form, 'P2. 体験したやり方', 'radio', {
    required: true,
    choices: ['A：画面の表示だけ（振動なし）', 'B：画面の表示＋手の振動']
  });

  form.addPageBreakItem().setTitle('使いやすさ');
  likertGrid_(form, 'このシステムについて答えてください',
    AGREE_HELP,
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
    range_(0, 10));

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

  // 条件 B 専用。分岐ではなく「任意回答」にしてある。
  // P2 で分岐させると SUS・TLX・リアルさまで 2 本に複製する羽目になり、
  // つけ間違えたときに回答が丸ごと飛ぶ。空欄なら集計で落とせばいいだけ。
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
    RATING_COLS, false);   // 必須にしない

  // 双極尺度。5 が「ちょうどよい」なので、他のグリッドに混ぜると平均が無意味になる。
  // 分析では |HP9 - 5| を「ちょうどよさからのズレ」として扱う。
  form.addScaleItem()
    .setTitle('HP9. 振動の強さはどうでしたか（5 = ちょうどよい）')
    .setBounds(0, 10)
    .setLabels('弱すぎる', '強すぎる')
    .setRequired(false);

  form.addPageBreakItem().setTitle('体験の後の自信');
  likertGrid_(form, '今のお気持ちについて答えてください', AGREE_HELP,
    SELF_EFFICACY_ROWS, RATING_COLS);

  form.addPageBreakItem().setTitle('今の体調');
  likertGrid_(form, '今の体調について答えてください', SSQ_HELP, SSQ_ROWS, RATING_COLS);

  form.addPageBreakItem().setTitle('自由に書いてください');
  addItem_(form, 'F1. よかったところ、役に立ったところを書いてください', 'paragraph', {});
  addItem_(form, 'F2. 使いにくかったところ、直してほしいところを書いてください', 'paragraph', {});
  addItem_(form, 'F3. おかしいと思ったところ（不具合）があれば書いてください', 'paragraph', {});

  form.setDestination(FormApp.DestinationType.SPREADSHEET, spreadsheetId);
  return form;
}

// ───────────────────────────────── 3.（削除）くらべるアンケート
//
// 被験者間計画にしたので、A/B を直接くらべる質問紙は成立しない。
// 参加者はどちらか一方しか体験しないため、「どちらが良かったか」に答えられない。
// A 群と B 群の差は、事後アンケートの各尺度を独立標本で比較して見る。

// ───────────────────────────────── 既存フォームの手直し

// 2026-11-02 に作ったフォームの ID（編集用 URL の /d/ と /edit の間）
var PRE_FORM_ID = '1FLUkhoAYiycuJbPG2Mefjfm6QdNUWAQLRVXmxmXSXmo';
var POST_FORM_ID = '19bcDQxGRkwu7HbYy3Vvx2RNuZlf_Lkyb_aDqr7dEhBY';
var COMPARE_FORM_ID = '1X72YP5FzarXnH_kSDWqSjTEa_btRo582f8yRVPkQ4q0';

/**
 * ★ これを実行する（URL は変わらない）。被験者間計画への変更をまとめて当てる：
 *
 *   共通   すべてのグリッドを 0〜10 の評価にそろえる（NASA-TLX に合わせる）
 *   事前   D4 を「学部 / 大学院 / その他」に差し替え、D5・D7・V3 を削除
 *   事後   P3「何回目ですか」を削除（1 人 1 回しか出さない）、
 *          P2 を「体験したやり方」に言い換え、HP9 を 0〜10 に（5 = ちょうどよい）
 *   比較   回答受付を停止してタイトルに【使用しない】を付ける
 *          （被験者間では A/B を直接くらべられないため。削除はしないので、
 *            要らなければ Drive で手で捨てること）
 *
 * 動作確認のダミー回答は消す。**本番の回答が 1 件でも入っていたら実行しないこと。**
 * 回答は復元できない。
 */
function applyAllChanges() {
  var log = [];

  log.push(patchForm_(PRE_FORM_ID, '事前', function (form) {
    applyPreFormEdits_(form);
  }));

  log.push(patchForm_(POST_FORM_ID, '事後', function (form) {
    var items = form.getItems();
    for (var i = items.length - 1; i >= 0; i--) {
      var title = items[i].getTitle() || '';

      if (title.indexOf('P3.') === 0) {
        form.deleteItem(items[i]);      // 被験者間なので「何回目」は無意味
        continue;
      }
      if (title.indexOf('P2.') === 0) {
        items[i].asMultipleChoiceItem().setTitle('P2. 体験したやり方');
      }
      if (title.indexOf('HP9.') === 0) {
        items[i].asScaleItem()
          .setTitle('HP9. 振動の強さはどうでしたか（5 = ちょうどよい）')
          .setBounds(0, 10)
          .setLabels('弱すぎる', '強すぎる');
      }
    }
  }));

  // 比較フォームは手直しせず、使えないようにして残す。
  // 消すのは取り返しがつかないので、捨てるかどうかは人間が決める。
  var cmp = FormApp.openById(COMPARE_FORM_ID);
  cmp.setAcceptingResponses(false);
  if (cmp.getTitle().indexOf('【使用しない】') !== 0) {
    cmp.setTitle('【使用しない】' + cmp.getTitle());
  }
  log.push('[比較] 回答受付を停止してタイトルに印を付けた（被験者間のため未使用）。' +
    '要らなければ Drive で手で削除: ' + cmp.getEditUrl());

  log.push(
    '\n【あと 1 手だけ手作業】\n' +
    'フォームの回答を消しても、スプレッドシート側の見出し行は残る。事前・事後それぞれで：\n' +
    '  1. スプレッドシートで該当タブを右クリック →「削除」\n' +
    '  2. フォームの「回答」タブ → 右上の ⋮ →「フォームのリンクを解除」\n' +
    '  3. 同じところで緑のシートアイコン → 既存のスプレッドシートを選んで再リンク\n' +
    'これで見出しが新しい質問構成で作り直される。'
  );

  Logger.log(log.join('\n'));
}

/** 回答を消す → 個別の手直し → グリッドを 0〜10 にそろえる、をまとめてやる。 */
function patchForm_(formId, label, extraEdits) {
  var form = FormApp.openById(formId);
  var n = form.getResponses().length;
  form.deleteAllResponses();

  extraEdits(form);
  var converted = convertGridsToRating_(form);

  return '[' + label + '] 回答 ' + n + ' 件削除 / グリッド ' + converted + ' 個を 0〜10 に / ' +
    form.getPublishedUrl();
}

/**
 * グリッドの列を 0〜10 に置き換え、説明文も合わせる。
 * 体調（SSQ）だけ文言が違うので行の中身で見分ける。
 */
function convertGridsToRating_(form) {
  var grids = form.getItems(FormApp.ItemType.GRID);
  for (var i = 0; i < grids.length; i++) {
    var g = grids[i].asGridItem();
    var rows = g.getRows();
    var isSsq = rows.length > 0 && rows[0].indexOf('SSQ1.') === 0;
    var isCompare = rows.length > 0 && rows[0].indexOf('C1.') === 0;
    var isTlx = rows.length > 0 && rows[0].indexOf('TLX1.') === 0;

    g.setColumns(RATING_COLS);

    if (isSsq) {
      g.setHelpText(SSQ_HELP);
    } else if (isCompare) {
      g.setHelpText('0 = A のほうがずっとよい　／　5 = どちらも同じ　／　10 = B のほうがずっとよい');
    } else if (isTlx) {
      g.setHelpText('0 = ぜんぜん　／　10 = とても');
    } else {
      g.setHelpText(AGREE_HELP);
    }
  }
  return grids.length;
}

function applyPreFormEdits_(form) {
  var dropPrefixes = ['D5.', 'D7.', 'V3.'];
  var items = form.getItems();
  var removed = [];
  var updated = [];

  // 後ろから回す。前から消すと添字がずれる。
  for (var i = items.length - 1; i >= 0; i--) {
    var title = items[i].getTitle() || '';

    if (hasAnyPrefix_(title, dropPrefixes)) {
      form.deleteItem(items[i]);
      removed.push(title);
      continue;
    }

    if (title.indexOf('D4.') === 0) {
      items[i].asMultipleChoiceItem()
        .setTitle('D4. 所属')
        .setChoiceValues(['学部', '大学院', 'その他']);
      updated.push(title + ' → D4. 所属');
    }
  }

  Logger.log([
    '削除: ' + (removed.length ? removed.join(' / ') : 'なし'),
    '変更: ' + (updated.length ? updated.join(' / ') : 'なし'),
    '確認: ' + form.getPublishedUrl()
  ].join('\n'));
}

function hasAnyPrefix_(text, prefixes) {
  for (var i = 0; i < prefixes.length; i++) {
    if (text.indexOf(prefixes[i]) === 0) return true;
  }
  return false;
}

// ───────────────────────────────── 共通処理

function applyCommonSettings_(form) {
  form.setCollectEmail(false);
  form.setAllowResponseEdits(false);
  form.setProgressBar(true);
  form.setShuffleQuestions(false);

  // 体験後アンケートは 1 人が 2 回出すので、1 回制限は必ず外す。
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
