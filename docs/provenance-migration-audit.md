# Audit of the former Add provenance metadata action

The former `FileActions.AddMetadata` action combined an independently supplied translated file, a previously generated XTM offline XLIFF, and current workflow assignments. The migration retains its attribution policy controls in `DownloadTranslatedInteroperableFile`: users can intentionally credit a workflow assignee for accepted machine translation even when no text changes were required. The audit concerns incorrect default selection, unreliable matching, and confusing that policy with an editor history.

The findings below describe the removed implementation. They are code-audit findings, not claims that every failure was reproduced against XTM. Live observations belong in the accompanying probe report.

## Assignment policy needed a clear meaning and a safer default

`AddMetadata` requested the job's workflow and assignment bundles. By default, it selected the last non-automatic workflow step without checking whether that step had started or finished. It then assigned each qualifying segment to the user currently assigned to the corresponding bundle. It did not read an editor identity or an editing event from the offline XLIFF.

A workflow assignment can intentionally identify the person credited with reviewing or accepting a translation. The `All` option is useful for exactly this case: an assignee may review machine translation, accept it unchanged, and deserve review credit. Requiring a text edit or an offline editor identity would remove that intended behavior.

The default still selected a future, unstarted review step when it happened to be the last manual step. That could credit work before the selected step had begun. The `OnlyConfirmed` option checked only `target/@state="signed-off"`; that state did not identify which user or workflow step performed the confirmation. The controls should therefore be described as attribution policies using assignments and segment state, rather than as a factual account of the last editor. Users should retain an explicit workflow-step override.

## Change detection did not establish human editing

The `OnlyChanged` option compared serialized target XML with the first matching `alt-trans` suggestion. An unrecognized qualifier selected the first alternative regardless of its origin. A missing alternative caused any nonempty target to count as changed.

An alternative is a suggestion, not a reliable previous revision. Multiple suggestions, a refreshed alternative, or differences in inline XML representation could change the comparison without identifying an editor. This comparison can support the explicit `OnlyChanged` policy, but should not be presented as editor history. When no workflow role was available, editing a nonempty suggestion also selected review provenance. Ordinary translation or machine-translation post-editing could consequently be labeled as review.

## Exports could be stale or inconsistent

The action downloaded `/projects/{projectId}/files/download` with `fileType=XLIFF`. It did not generate a fresh export, wait for generation, or bind the download to a generated file ID. A current translated file could be combined with an older offline export. The first XLIFF entry in the returned ZIP was accepted without verifying that the archive contained exactly one candidate.

The combined download must request TARGET and offline XLIFF generation before it begins waiting. Each download should use its returned file ID. Starting both jobs together reduces waiting time, although two independent generation requests do not constitute an atomic snapshot if someone edits the job during generation.

## Segment matching was incomplete

The implementation flattened source segments from both files and matched them by position. It compared only source text after `NormalizeElementText` removed inline structure and collapsed all whitespace. The comparison ignored offline unit IDs, file scope, target language, translated target content, and significant whitespace preserved with `xml:space`.

Distinct units with identical source text could pass the check in the wrong order. A different target-language file or an outdated target could also pass because its source text remained unchanged. Conversely, the global unique-ID requirement rejected XLIFF 2 documents whose separate `file` elements reused otherwise valid unit IDs.

The translated-side segment count included `ignorable` elements and units with `translate="no"`. XTM's offline export omits excluded content, so those files could fail the equal-count check. The earlier local probe in `artifacts/xliff-translate-no-probe/export-inspection.json` records excluded source text and unit IDs as absent from offline exports.

## Aggregation and replacement lost provenance

For a unit containing several segments, the first qualifying bundle assignee became the person for the entire unit, and the first state qualifier became its specific tool. Later conflicting contributors or qualifiers were ignored. This could represent mixed work as the contribution of one person or tool.

The selected translation or review provenance was then overwritten, including clearing `PersonReference` and `ToolReference`. Even an export containing no person evidence could erase existing person information. Excluded units need to retain their existing provenance, and any replacement policy for translated units must be explicit.

## Previous tests did not cover difficult attribution cases

`FileActionsTests.AddMetadata_LiveProject_AppliesProvenancePriority` used a fixed existing project and expected the same assigned user. It covered the attribution modes, including the intentional policy of crediting an assignee for unchanged segments. `DataSources.ManualWorkflowStepDataHandlerReturnsOnlyManualSteps` checked a fixed workflow. Neither test separated assignment policy from editor history or exercised different users and workflow steps.

They did not cover future or reassigned workflow steps, missing editor evidence, mixed contributors within one unit, duplicate source text, excluded units, wrong target languages, stale targets, or multiple XLIFF file scopes. The old action test was removed with `AddMetadata`; the manual-step data-source test now uses `DownloadTranslatedInteroperableFileRequest`. Tests for the combined download should verify both the offline evidence and the configured assignment policy, including human review of accepted machine translation with no text edit.
