# Quest System Overview

This file explains how the current quest pipeline is wired and how to extend it—for example, by attaching lore entries or other narrative payloads to each quest.

---

## 1. Core Building Blocks

| File | Responsibility |
| --- | --- |
| `QuestBase` (Scriptable Object) | Authoring data: quest name/description, objectives (item + quantity), reward item or recipe. |
| `Quest` (runtime wrapper) | Tracks progress/completion status for a single `QuestBase`. Handles reward payout when completed. |
| `QuestManager` (MonoBehaviour) | Lives on the player or a global manager. Owns the current quest instance, runs progress updates, and grants rewards. |
| `QuestgiverManager` (MonoBehaviour) | Sits on an NPC/UI. Shows quest text, sends accepts/rejects back to the player’s `QuestManager`. |

### 1.1 Authoring Quests
1. Right‑click in the Project window → `Create > Scriptable Objects > QuestBase`.
2. Fill in:
   - `Quest Details`: name/description for display.
   - `Quest Objectives`: for each target item set the `ItemBase` reference and quantity required. `currentQuantity`/`objectiveComplete` are runtime fields; leave them at 0/false.
   - `Rewards`: specify an `ItemBase` and quantity and/or a `CraftingRecipieBase`.
3. Optionally create multiple `QuestBase` assets to represent different quest chains.

### 1.2 Runtime Flow
1. Player interacts with a quest giver:
   - `QuestgiverManager` lists objectives via `QuestObjective.GetQuestObjectiveText()`.
   - Accept button calls `QuestManager.AddQuest(QuestBase questBase)`.
2. `QuestManager` wraps the ScriptableObject in a runtime `Quest` and immediately checks progress (`CheckQuestCompletion`).
3. Each time the player’s inventory changes, the quest manager should run `UpdateCurrentQuest()` (hook this into inventory events or call it on a timer/button).
4. When all objectives report `objectiveComplete == true`, `QuestManager.CompleteQuest()` grants rewards and clears `currentQuest`.

---

## 2. Integrating With UI & Inventory

### Inventory Checks
`QuestObjective.UpdateQuestObjective(Inventory inventory)` calls:
- `inventory.GetItemQuantity(item)` → updates `currentQuantity`.
- `inventory.HasItem(item, quantity)` → updates `objectiveComplete`.

This means quests currently rely on collecting items. To trigger quests from other events (kills, locations, etc.), you can:
- Extend `QuestObjective` with additional fields (e.g., `ObjectiveType` enum) and update logic accordingly.
- Inject external progress by writing a helper that calls `Quest.questProgress = ...` manually for non-item objectives.

### UI Interaction
Quest UI is minimal:
- `QuestgiverManager` populates a single `TMP_Text` block with the quest name and objective lines.
- Accept/Reject buttons toggle the quest giver panel and notify `QuestManager`.

To show progress to the player while the quest is active, add a HUD widget that reads from `QuestManager.currentQuest.quest.questObjectives` and prints `GetQuestObjectiveText()` with updated counts.

---

## 3. Extending the System (Adding Lore)

To attach lore or other narrative metadata, follow these steps:

1. **Augment `QuestBase`:**
   ```csharp
   [TextArea]
   public string loreEntry;
   public AudioClip loreVO;
   public List<Sprite> loreImages;
   ```
   - `loreEntry`: Long-form text, diaries, faction history, etc.
   - Optional multimedia fields (voice lines, concept art) you want to unlock with the quest.

2. **Expose Lore in UI:**
   - In `QuestgiverManager`, add extra `TMP_Text`/image slots to show lore when a player highlights/accepts the quest.
   - Store unlocked lore in a `LoreCodex` ScriptableObject or a simple `List<string>` on a new `LoreManager`.

3. **Persist Unlocks:**
   - Modify `QuestManager.CompleteQuest()` to notify your `LoreManager`, e.g.:
     ```csharp
     if (!string.IsNullOrEmpty(currentQuest.quest.loreEntry))
     {
         LoreManager.Instance.UnlockEntry(currentQuest.quest);
     }
     ```
   - Save unlocked lore using your existing save system (PlayerPrefs, JSON, etc.).

4. **Add New Quest Types:**
   - Introduce an enum on `QuestBase` (e.g., `QuestCategory { MainStory, SideStory, Lore }`) so the UI can group or filter quests.
   - Lore quests can skip rewards and simply reveal narrative content upon completion.

5. **Dialogue Hooks:**
   - If you have a dialogue system, store a Dialogue Graph reference on `QuestBase`.
   - When a quest is accepted/completed, trigger the associated dialogue to deliver the lore.

---

## 4. Tips & Future-Proofing

- **Multiple Quests:** `QuestManager` currently tracks only one quest (`currentQuest`). To support several simultaneous quests, switch `currentQuest` to `List<Quest>` and update UI/progress loops accordingly.
- **Non-Item Objectives:** Add additional fields (kill counts, location triggers, timers) to `QuestObjective`, or create separate structs and a base interface (e.g., `IQuestCondition`). Keep the item-based logic for harvest/fetch quests while layering new types.
- **Networking/Save Data:** Serialize `Quest` state (`questBase GUID`, progress %, objective completion) if you plan to save/load mid-quest or sync across sessions.
- **Tooling:** Consider a custom editor for `QuestBase` to preview lore text, reorder objectives, and validate missing fields.

With these extensions, you can turn the existing fetch-quest framework into a flexible narrative delivery system—either by sprinkling lore across side quests or by building full story arcs that chain multiple `QuestBase` assets together.

