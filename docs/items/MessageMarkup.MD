# RE7 Message Markup

RE7 `.msg.17` files store UTF-16 message strings with RE Engine `via.gui` message tags embedded in angle brackets. RE7 also runs an application-level replacement pass for some game-specific tags, most visibly input icons.

This document describes the markup that appears in the extracted vanilla RE7 message corpus and the tag types exposed by the engine enum but not observed in vanilla RE7 strings.

## Observed Authoring Subset

Start with these observed forms for BioRand-authored text. Their presence in vanilla messages does not establish support in every UI; use `PAGE`, `TIME`, and `REF` only in appropriate message contexts as described below.

```text
plain Unicode text
line breaks (actual CRLF characters)
<COLOR RRGGBB>Colored texts</COLOR>
<SIZE n>Custom font size</SIZE>
<CENTER>Centered text</CENTER>
<ICON NAME>
<PAGE>
<TIME start,end>
<REF message_name>
```

Practical rules:

- Prefer uppercase tag names. Vanilla RE7 overwhelmingly uses uppercase tags.
- Prefer `<COLOR RRGGBB>` without `#`.
- Always close range-style tags such as `COLOR`, `SIZE`, and `CENTER`.
- Do not use tags inferred only from engine enum names in new randomizer text without runtime testing in the target UI.
- Treat unknown angle-bracket text as risky in UI strings. Some subtitle/editorial text uses literal angle brackets, but recognized tag names can change rendering.

## Observed RE7 Tags

| Tag | Observed count | Syntax | Closing tag | Notes |
| - | -: | - | - | - |
| `PAGE` | 14298 | `<PAGE>` | No | Page break. IDA shows `getPageCount` counts the exact wide string `<PAGE>`. |
| `TIME` | 91633 | `<TIME start,end>` or `<TIME start end>` | No | Subtitle/message timing. IDA shows comma or space delimiters are accepted. |
| `ICON` | 7749 | `<ICON NAME>` | No | Game-level replacement/render tag for input icons. Not listed in `via.gui.MessageTag.Type`; likely handled through `app.MessageTagReplaceData` or adjacent UI replacement logic. |
| `SIZE` | 3133 | `<SIZE n>` | `</SIZE>` | Font size override. |
| `CENTER` | 761 | `<CENTER>` | `</CENTER>` | Center alignment. Many vanilla strings open `CENTER` without an observed close in the same string, so UI scope can be context-sensitive. Close it in authored text. |
| `COLOR` | 694 | `<COLOR RRGGBB>` | `</COLOR>` | Text color override. Vanilla also contains one lowercase legacy form, described below. |
| `REF` | 176 | `<REF message_name>` | No | Message reference/include by message entry name. |

False positives from the corpus scan included editorial/subtitle notes such as `<heavy breath>`. These are literal text in message data, not known engine tags.

## Color

Preferred form:

```text
<COLOR FFFF00>yellow text</COLOR>
```

Observed color openings:

| Form | Count | Notes |
| - | -: | - |
| `<COLOR FFFF00>` | 504 | Common Birthday/item highlight color. |
| `<COLOR FF0000>` | 103 | Red. |
| `<color #FF8888>` | 40 | Lowercase legacy/Unity-like form found in one Daughters scenario file. Avoid for new text unless runtime-tested. |
| `<COLOR FFFFFF>` | 33 | White. |
| `<COLOR FF5A00>` | 14 | Orange. |

Observed color closings:

| Form | Count |
| - | -: |
| `</COLOR>` | 621 |
| `</color>` | 40 |

Authoring notes:

- Use six hexadecimal RGB digits: `RRGGBB`.
- Do not include alpha unless runtime-tested; vanilla observed forms are RGB only.
- Do not assume lowercase tags are generally supported. Use uppercase.

## Size

Preferred form:

```text
<SIZE 18>small text</SIZE>
```

Observed `SIZE` arguments:

```text
12, 14, 15, 16, 18, 19, 20, 21, 22, 23, 24, 25, 26, 35
```

There is one malformed-looking vanilla occurrence of `<SIZE>` with no argument in `ui_map_mes.msg.17`. Treat that as data noise or a very context-specific string, not as an authoring pattern.

## Time

Preferred form:

```text
<TIME 0,2.0>Timed subtitle text
```

IDA behavior:

- `getPageTime` searches for `<TIME `, then the next `>`.
- It parses two floating point values.
- It accepts comma-separated or space-separated values.
- It returns start time plus duration, where duration is `end - start`.
- `getTotalTime` scans all `TIME` tags and returns the maximum end time.

Observed forms include:

```text
<TIME 0, 2.0>
<TIME 0,2.0>
<TIME 0.0, 2.0>
<TIME 2.0 4.0>
```

For new text, use comma-separated form without relying on extra spaces:

```text
<TIME 0,2.0>
```

## Page

`<PAGE>` is an exact marker. IDA shows page counting is literal and case-sensitive at the helper level:

```text
First page<PAGE>Second page
```

Use this only in UIs that support paged message display. Inventory descriptions may not expose page navigation in every context.

## Center

Preferred form:

```text
<CENTER>Centered text</CENTER>
```

Vanilla has many `<CENTER>` openings and fewer `</CENTER>` closings. That suggests some call sites may reset alignment at page/string boundaries, but authored randomizer text should close the tag to avoid leaking alignment through later text.

## Icons

Preferred form:

```text
Press <ICON DECIDE> to continue.
```

Observed icon names:

```text
BULLET_CHANGE01_PC
BULLET_CHANGE02_PC
BULLET_CHANGE03_PC
BULLET_CHANGE_PC
CAMERA_PC
CANCEL
CARD_DRAW_PC
CARD_UNDRAW_PC
COMBINE
C_L
DECIDE
DESCRIPTION_PC
DISPLAY_CHANGE_PC
FLOOR_DOWN_PC
FLOOR_UP_PC
INTERACT
ITEM_MOVE_PC
KEY_DEL
KEY_ESC
LEFT_PUNCH_PC
LS
LS_D_FIX
LS_L_FIX
LS_P
LS_PC
LS_P_PC
LS_R_FIX
LS_U
LS_U_FIX
LT_B
LT_B_FIX
LT_B_PC
LT_T
LT_T_FIX
LT_T_PC
NOW_POSITION_PC
PLUS
POV
POV_Cursor
POV_L
POV_L_Cursor
POV_R
POV_R_Cursor
QUICK_TURN_PC
RIGHT_PUNCH_PC
ROTATION_PC
RS
RS_P
RS_PC
RS_P_PC
RT_B
RT_B_FIX
RT_B_PC
RT_T
RT_T_FIX
RT_T_PC
R_L
R_R
R_U
SEARCH
SORT_PC
SP_CHECK_PC
SP_USE_PC
TOUCHPAD_CLICK
```

`ICON` is not a `via.gui.MessageTag.Type` enum value. It should be treated as an RE7/game-level replacement tag rather than a pure engine tag.

## References

Preferred form:

```text
<REF c00_em2050_1500_0200>
```

Observed reference targets:

```text
c00_em2050_1500_0200
c31_em3350_0010_3200
c41_em9950_0350_0100
c74_em3150_4040_0100
c74_em3150_4040_0200
c74_em3150_4060_0100
c74_em3150_4060_0200
c74_em3150_4060_0300
c74_em3150_4160_0100
c74_em3150_4160_0200
c74_em3150_4160_0300
CH8_pl1050_2102_0500
CH8_em9452_2102_0400
```

Use `REF` only when the referenced message entry is guaranteed to exist in the same loaded message context.

## Engine MessageTag Enum

`Enums.via.gui.MessageTag.Type` exposes these tag types. "Observed" means present in the extracted RE7 `.msg.17` corpus. "Unobserved enum" means the enum value exists, but no vanilla RE7 message string in the scanned corpus used the corresponding angle tag. Enum presence alone does not prove the suggested spelling, argument syntax, or support in an RE7 UI.

| Enum value | Likely text tag | Status in RE7 corpus | Notes |
| - | - | - | - |
| `Unknown` | N/A | Internal | Fallback enum value. |
| `Page` | `<PAGE>` | Observed | Exact page marker. |
| `Line` | `<LINE>` | Unobserved enum | Not observed in scanned RE7 strings. |
| `Size` | `<SIZE ...>` | Observed | Font size override. |
| `Font` | `<FONT ...>` | Unobserved enum | Not observed in scanned RE7 strings. |
| `Color` | `<COLOR ...>` | Observed | RGB text color override. |
| `Char` | `<CHAR ...>` | Unobserved enum | Not observed in scanned RE7 strings. |
| `Space` | `<SPACE ...>` | Unobserved enum | Not observed in scanned RE7 strings. |
| `Wordwrap` | `<WORDWRAP ...>` | Unobserved enum | Not observed in scanned RE7 strings. |
| `Center` | `<CENTER>` | Observed | Center alignment. |
| `Left` | `<LEFT>` | Unobserved enum | Not observed in scanned RE7 strings. |
| `Right` | `<RIGHT>` | Unobserved enum | Not observed in scanned RE7 strings. |
| `Top` | `<TOP>` | Unobserved enum | Not observed in scanned RE7 strings. |
| `Bottom` | `<BOTTOM>` | Unobserved enum | Not observed in scanned RE7 strings. |
| `Time` | `<TIME ...>` | Observed | Timing ranges. |
| `Ruby` | `<RUBY ...>` | Unobserved enum | Ruby/furigana-related; not observed in scanned RE7 strings. |
| `RubyB` | `<RUBYB ...>` | Unobserved enum | Ruby-related; not observed in scanned RE7 strings. |
| `RubyRB` | `<RUBYRB ...>` | Unobserved enum | Ruby-related; not observed in scanned RE7 strings. |
| `RubyRT` | `<RUBYRT ...>` | Unobserved enum | Ruby-related; not observed in scanned RE7 strings. |
| `Event` | `<EVENT ...>` | Unobserved enum | Not observed in scanned RE7 strings. |
| `TCU` | `<TCU ...>` | Unobserved enum | Not observed in scanned RE7 strings. |
| `Draw` | `<DRAW ...>` | Unobserved enum | Rendering control; not observed in scanned RE7 strings. |
| `GlowDraw` | `<GLOWDRAW ...>` | Unobserved enum | Rendering control; not observed in scanned RE7 strings. |
| `ShadowDraw` | `<SHADOWDRAW ...>` | Unobserved enum | Rendering control; not observed in scanned RE7 strings. |
| `Sprite` | `<SPRITE ...>` | Unobserved enum | Sprite insertion; not observed in scanned RE7 strings. |
| `Baseline` | `<BASELINE ...>` | Unobserved enum | Text baseline control; not observed in scanned RE7 strings. |
| `Glow` | `<GLOW ...>` | Unobserved enum | Glow styling; not observed in scanned RE7 strings. |
| `Shadow` | `<SHADOW ...>` | Unobserved enum | Shadow styling; not observed in scanned RE7 strings. |
| `Italic` | `<ITALIC>` | Unobserved enum | Not observed in scanned RE7 strings. |
| `Bold` | `<BOLD>` | Unobserved enum | Not observed in scanned RE7 strings. |

Do not infer the full argument grammar for unobserved tags from the enum name alone. `via.gui.GUIUtility::ParseMessageTagArgs` can parse named and quoted arguments, but vanilla RE7 strings mostly use simple positional arguments such as `<COLOR FFFF00>` and `<SIZE 18>`.

## MessageTag Attribute Flags

The engine also exposes `Enums.via.gui.MessageTag.Attr`:

| Attr value | Notes |
| - | - |
| `None` | No special attribute flag. |
| `PopEnd` | Likely closes or pops a tag scope. |
| `BaseColor` | Color-related internal flag. |
| `Icon` | Icon-related internal flag. |
| `Ruby` | Ruby/furigana-related internal flag. |
| `TCU` | TCU-related internal flag. |
| `IconMerge` | Icon merge/internal rendering flag. |

These are internal attribute flags, not necessarily authorable angle tags.

## CSV And Serialization Notes

BioRand CSV fields that contain message markup should preserve the raw message string. The important hazards are CSV escaping, not MSG escaping:

- Quote fields that contain commas, quotes, or line breaks.
- Double embedded CSV quotes: `"` becomes `""`.
- Use actual line breaks inside quoted CSV fields. `Csv.Deserialize` and `MessageModifier` do not expand literal `\r\n` or `\n` escape sequences. C# string literals and JSON have their own escape processing before a message reaches the writer.
- Do not HTML-escape angle brackets. The game expects literal `<` and `>`.
- Keep message tags inside the same quoted CSV field.

Example CSV field with an actual line break:

```csv
"Raises attack power.
<COLOR FFFF00>Stacks with other skills.</COLOR>"
```

When serialized back to `.msg.17`, the REE MSG writer stores the string as UTF-16 string data and encrypts the string block. The tag text itself is not transformed by the serializer; rendering behavior is decided later by RE Engine GUI/message code.
