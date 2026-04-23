using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs;
using RusalProject.Models.Entities;
using RusalProject.Models.Types;
using RusalProject.Provider.Database;

namespace RusalProject.Services.EditorHotkeys;

public class UserEditorHotkeysService : IUserEditorHotkeysService
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	private static readonly HashSet<string> ModifierCodes = new(StringComparer.Ordinal)
	{
		"ControlLeft", "ControlRight",
		"ShiftLeft", "ShiftRight",
		"AltLeft", "AltRight",
		"MetaLeft", "MetaRight",
	};

	private readonly ApplicationDbContext _context;

	public UserEditorHotkeysService(ApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<EditorHotkeysResponseDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		var row = await _context.UserEditorHotkeys.AsNoTracking()
			.FirstOrDefaultAsync(h => h.UserId == userId, cancellationToken);

		Dictionary<string, EditorHotkeyChordDto?> stored = new(StringComparer.Ordinal);
		if (row is not null && !string.IsNullOrWhiteSpace(row.BindingsJson) && row.BindingsJson != "{}")
		{
			try
			{
				stored = JsonSerializer.Deserialize<Dictionary<string, EditorHotkeyChordDto?>>(row.BindingsJson, JsonOptions)
				         ?? new Dictionary<string, EditorHotkeyChordDto?>();
			}
			catch (JsonException)
			{
				stored = new Dictionary<string, EditorHotkeyChordDto?>();
			}
		}

		var merged = new Dictionary<string, EditorHotkeyChordDto?>(StringComparer.Ordinal);
		foreach (var id in EditorHotkeyActionCatalog.All)
		{
			if (stored.TryGetValue(id, out var chord))
				merged[id] = chord;
			else
				merged[id] = null;
		}

		return new EditorHotkeysResponseDto { Bindings = merged };
	}

	public async Task SaveAsync(Guid userId, SetEditorHotkeysDto dto, CancellationToken cancellationToken = default)
	{
		if (dto.Bindings is null)
			throw new ArgumentException("Bindings are required.", nameof(dto));

		var missing = EditorHotkeyActionCatalog.All.Where(id => !dto.Bindings.ContainsKey(id)).ToList();
		if (missing.Count > 0)
		{
			throw new ArgumentException(
				"В теле запроса нужно передать bindings для всех действий. Не хватает ключей: "
				+ string.Join(", ", missing));
		}

		var unknown = dto.Bindings.Keys.Where(k => !EditorHotkeyActionCatalog.AllSet.Contains(k)).ToList();
		if (unknown.Count > 0)
			throw new ArgumentException("Неизвестные действия: " + string.Join(", ", unknown));

		var normalized = new Dictionary<string, EditorHotkeyChordDto?>(StringComparer.Ordinal);

		foreach (var id in EditorHotkeyActionCatalog.All)
		{
			var chord = dto.Bindings[id];
			if (chord is null)
			{
				normalized[id] = null;
				continue;
			}

			ValidateChord(chord);
			normalized[id] = chord;
		}

		EnsureNoDuplicateChords(normalized);

		var nonNull = normalized.Where(p => p.Value is not null)
			.ToDictionary(p => p.Key, p => p.Value!, StringComparer.Ordinal);

		var entity = await _context.UserEditorHotkeys
			.FirstOrDefaultAsync(h => h.UserId == userId, cancellationToken);

		if (nonNull.Count == 0)
		{
			if (entity is not null)
			{
				_context.UserEditorHotkeys.Remove(entity);
				await _context.SaveChangesAsync(cancellationToken);
			}

			return;
		}

		var json = JsonSerializer.Serialize(nonNull, JsonOptions);
		if (entity is null)
		{
			entity = new UserEditorHotkeys { UserId = userId, BindingsJson = json };
			_context.UserEditorHotkeys.Add(entity);
		}
		else
		{
			entity.BindingsJson = json;
		}

		await _context.SaveChangesAsync(cancellationToken);
	}

	private static void ValidateChord(EditorHotkeyChordDto chord)
	{
		if (string.IsNullOrWhiteSpace(chord.Code))
			throw new ArgumentException("У хоткея должен быть указан code (физическая клавиша).");

		if (ModifierCodes.Contains(chord.Code))
			throw new ArgumentException("Нельзя назначить только модификатор без основной клавиши.");
	}

	private static void EnsureNoDuplicateChords(Dictionary<string, EditorHotkeyChordDto?> map)
	{
		var seen = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var (actionId, chord) in map)
		{
			if (chord is null)
				continue;

			var sig = ChordSignature(chord);
			if (seen.TryGetValue(sig, out var other) && other != actionId)
				throw new ArgumentException(
					$"Комбинация уже назначена на другое действие ({other} и {actionId}).");

			seen[sig] = actionId;
		}
	}

	private static string ChordSignature(EditorHotkeyChordDto c) =>
		$"{c.Code}\u001f{c.CtrlKey}\u001f{c.ShiftKey}\u001f{c.AltKey}\u001f{c.MetaKey}";
}
