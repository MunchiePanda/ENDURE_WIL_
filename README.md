## ENDURE_WIL_

### Rules and Naming Conventions

- **Branch naming**: Use short prefixes by deliverable. Example: `S1_Item` where `S` stands for Initial/Setup.

- **Deliverable codes**:
  - **1**: Procedural Generation
  - **2**: User Interface
  - **3**: Visuals (Scene setup, animation, etc.)
  - **4**: Player Systems (Movement, Stats, Attributes)
  - **5**: Items, Inventory, Crafting
  - **6**: Interactables
  - **7**: Enemies & AI

- **Naming Conventions**:
  - **Booleans**: b prefix, `bool bRunning`
  - **Arrays/lists**: plural nouns, `GameObject[] enemies`
  - **Variables**: camelCase, `float jumpSpeed`
  - **Properties**: PascalCase, `public float JumpSpeed { get; set; }`
  - **Methods**: PascalCase, `void AddItem()`

- **Debug Log**
  - Format: `ClassName MethodName(): Message (Deliverable Codes)`
  - Example: `InventoryUIManager AddItem(): Can't add item (D2, D5)`

- **Comments (describe what and why only if it isnt self-explanatory)**:
  - Above methods, describe what they do and why.
  - For if statements, state what is being checked and the action (and why).
  - For loops, describe what they do and why.
  - Call out unclear or shorthand code; explain briefly.
  - If code needs revision or is relevant to another deliverable, @mention the teammate, note the deliverable, and sign with `~YourName`. Example: `@Mik 2 UI - Use these delegates ~Sio`

Keep code readable and consistent. Prefer clarity over cleverness. When in doubt, add a brief comment that explains intent and reasoning.

### Examples

```csharp
// If-statement with WHY
// If quantity is less than or equal to 0, remove the item to keep inventory clean
if (items[item] <= 0)
{
    items.Remove(item);
    OnItemRemoved?.Invoke(item, oldQuantity);
}
else // If quantity remains above 0, update observers with the new quantity
{
    OnItemQuantityChanged?.Invoke(item, oldQuantity, items[item]);
}
```

```csharp
// Shorthand clarification
if (items[item] >= quantity) // Shorthand for: if (items[item].quantity >= quantity)
{
    // ...
}
```

```csharp
// Cross-deliverable note with @mention
// Events for inventory changes (Delegates)
// @Mik 2 UI - Use these delegates to refresh the inventory UI ~Sio
public System.Action<ItemBase, int> OnItemAdded;
```
