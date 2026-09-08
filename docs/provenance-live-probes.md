# XTM offline provenance probes

Run date: 2026-09-08. API contract: local `artifacts/xtm-docs.json`, XTM REST API 26.3. Credentials: existing `Tests.XTM/appsettings.json`, read without logging passwords or tokens. The docs file contains the OpenAPI contract, not credentials.

## Scope and evidence

Two new projects containing synthetic text were created with all email notifications disabled. No existing project or user was edited.

| Project | Job | Workflow | Purpose |
| --- | --- | --- | --- |
| 2857236 | 2857246 | `translate # review` | Empty targets, changed targets, unchanged confirmation, read-only review behavior |
| 2857304 | 2857315 | `translate -> correct` | Actual correction edits, unchanged approval, fuzzy TM evidence |

Both source files are XLIFF 2.1 with five units and six segments. `unit-multi` has two segments. Unit/segment IDs remain intact in the TARGET export. Offline XLIFF is 1.2 and assigns `t1` through `t6`; the two segments of `unit-multi` appear together under generated group `g5`. No original unit ID, segment ID, or `resname` appears in the offline file. Existing earlier live archives independently show the same ID replacement for XLIFF 1.2, 2.0, and 2.1 inputs.

Sanitized snapshots are in `Tests.XTM/TestFiles/Input/Provenance/live-*.xlf`. They preserve segment and alternative-translation XML, remove unrelated metrics from the header, and replace the API account ID in `xtm:changedby` with `1001`. Raw synthetic exports, request/status history, and scripts remain under ignored `artifacts/`; no tokens or passwords are stored there by these probes.

## Documentation checked

- [XTM offline XLIFF instructions](https://xtm-cloud.atlassian.net/wiki/spaces/EXKB/pages/3273492047/How+to+translate+offline+use+of+XLIFF+files): offline XLIFF and source XLIFF are different artifacts. `XLIFF` populates otherwise empty targets from source; `XLIFF_NTP` leaves them empty. Upload accepts a workflow step and an approval policy.
- [XTM file generation](https://help.xtm.ai/en/xtm-cloud/26.3/en/project---files-popup.html): offline exports use XLIFF 1.2. Historical target generation can select a workflow step.
- [XTM offline upload](https://help.xtm.ai/en/xtm-cloud/25.6/en/uploading-xliff-and-multi-xliff-files-in-the-project-editor---files-tab-screen.html): approval can apply to no segments, statuses in the XLIFF, changed segments, or all translated segments. Administrators can change status mappings.
- [Default workflow steps](https://help.xtm.ai/en/xtm-cloud/26.3/en/default-workflow-steps-in-xtm-cloud.html): Translate and Correct can edit target text. Review accepts comments and cannot edit translations.

The checked docs do not promise an editor audit trail in offline XLIFF. The observations below concern this tenant's configuration and API uploads; they do not establish behavior for every tenant or Workbench edit path.

## Generation and workflow API findings

The generate endpoint has a singular `fileType` query parameter. Each successful snapshot requested `XLIFF` and `TARGET` generation before making any generation-status request. Both jobs were already `FINISHED` on the first poll in these small files. This validates the call ordering, not a particular performance improvement on larger jobs.

Passing `referenceStepName=translate1` to offline `XLIFF` generation returned HTTP 403 with the exact error:

> Workflow step can only be specified for the TARGET file type.

Use workflow selectors only for TARGET generation. Offline XLIFF cannot be assumed to describe a requested historical target step.

`GET /projects/{projectId}/status?fetchLevel=STEPS` returned per-job steps with `workflowStepName`, `displayStepName`, `stepReferenceName`, `referenceStepName`, and `status`. The completed step used `FINISHED` and a millisecond `finishDate`; the current step used `IN_PROGRESS`. Workflow definitions and workflow assignments do not contain that active-step status. Assignment bundles use inclusive segment positions (`from: 1`, `to: 6` in these jobs), not the source XLIFF unit IDs.

The documented upload enums are:

- `xliffOptions.autopopulation`: `ENABLED`, `DISABLED`.
- `xliffOptions.segmentStatusApproving`: `NONE`, `ACCORDINGLY_TO_STATE`, `ALL_UPDATED_SEGMENTS`, `ALL_SEGMENTS`.

Uploads here used `autopopulation=DISABLED` and an explicit reference step. Upload status was awaited before subsequent exports, except the first translate upload whose completion was established by the changed content in its following export.

## Observed edits and attribution

| Snapshot / operation | Observed offline result |
| --- | --- |
| Initial source with an empty target | `XLIFF` copies source into target, with no target `state`. This is export population, not evidence of a human translation. |
| Initial source with a prefilled target | `state="translated"`, before any probe editor acted. |
| Translate: change existing target | Changed text is exported with `state="translated"`. No current editor identity, modification timestamp, prior target, or changed flag appears. |
| Translate: create first translation | New text is exported with `state="translated"`. Without a baseline or alternative target, this cannot be distinguished from an initial prefilled translation. |
| Translate: unchanged target marked `signed-off` on upload | Export remains `state="translated"` while a later review step is incomplete. |
| Move workflow to Review without editing | Exported target metadata remains unchanged. |
| Review: attempted text edits plus signed-off statuses | Upload reports `FINISHED`, but target text edits are ignored. Previously completed translation segments become `signed-off`, including unchanged targets. Segments incomplete in the previous step stay `translated`. |
| Review: unchanged upload with `ALL_SEGMENTS` | Same signed-off subset: this policy does not override incomplete previous-step status. |
| Correct: change two targets with `ALL_UPDATED_SEGMENTS` | Changed text is accepted. Only those two targets export `signed-off`; four unchanged targets remain `translated`. |
| Correct: unchanged upload with `ALL_SEGMENTS` | All six targets become `signed-off`, including unchanged inherited targets. The TM alternative author metadata remains unchanged. |

No current-segment person or edit timestamp appeared in either probe. The only author-like attributes observed were `xtm:changedby` and `xtm:changedate` on `alt-trans` elements representing fuzzy TM matches from the earlier synthetic project. Their values remained unchanged after correction edits. They identify the matched TM record's updater, not the current segment's editor. The qualifier also remained `fuzzy-match` after editing.

The second project picked up fuzzy matches from the first project's approved synthetic records (84–87% match quality because the source timestamps differ). This provides a useful distinction: a current target can differ from its TM alternative before any current-project edit. A target can also equal the alternative after an actual edit. An alternative-target comparison measures difference from that candidate, not a complete current-step edit history.

## Implications for provenance and the original action

`All segments` is an attribution policy: it can credit the assigned human reviewer who accepts unchanged machine translation. Offline author history is not required for that policy, and lack of text changes must not prevent it. The emitted person should be described as an assigned contributor under the selected policy, not as a verified last editor.

The original implementation's assumptions need these qualifications:

1. Downloading a previously generated offline file can use stale data or fail if generation never ran. Generate both artifacts before waiting.
2. A nonempty target without `alt-trans` does not prove that a user created it from empty. Prefilled source targets and automatic target population are counterexamples from the probes.
3. `signed-off` is affected by workflow completion and administrator status mapping. It does not identify a person or prove a text edit. Unchanged approval can legitimately become signed-off.
4. `alt-trans` author fields describe the TM candidate. They must not silently become current-segment editor attribution.
5. Comparing the current target with an alternative target provides limited change evidence, not a step-by-step audit trail.
6. Selecting an assignee from a future workflow step can credit someone before that step starts. Use per-job step status when choosing the default attribution step.
7. Offline `tN` IDs cannot be matched directly to original XLIFF unit IDs. Mapping must preserve file order and validate source segment content and structure, including multiple segments per unit.

Only one authenticated account was available. These probes do not establish how exports differ between two actual editors, UI Workbench edits, MT engines, or custom workflow/status configurations. The evidence supports policy attribution to workflow assignees; it does not support an unqualified claim of per-segment last-editor provenance.

After the snapshots were captured, the authenticated account was assigned to both steps in each new project for end-to-end action tests. The API returned `success: true`. Project email notifications remained disabled. During the action tests, the review project was in `review1`; the correction project was in `correct1`, with all six segments signed off after unchanged approval. These assignments are policy inputs, not additional evidence of who made earlier edits.

## Reproduction

The integration action was also tested against XTM, beyond the direct API probes:

- Existing TXT, HTML, and XLIFF roundtrip tests passed, including unit-level provenance and excluded-content assertions (80 seconds).
- The assigned-reviewer action test passed `All segments`, `Only confirmed segments`, and `No segments` policies against job `2857315`, including accepted unchanged translations (8 seconds for the original run).
- After tightening default workflow selection to allow only documented manual step types, the same reviewer action test passed again: 1 test, 0 failures, 9 seconds. Command: `XTM_PROVENANCE_PROJECT_ID=2857304 XTM_PROVENANCE_JOB_ID=2857315 dotnet test Tests.XTM/Tests.XTM.csproj --filter FullyQualifiedName~Download_AcceptedUnchangedSegments_AttributesAssignedReviewer --no-restore -v:q -p:WarningLevel=0`.

The ignored `artifacts/probe-provenance.mjs` script obtains credentials from test settings and refuses to create a duplicate project when its saved state already has a project ID. `XTM_PROVENANCE_OUTPUT` selects a separate probe state directory. Its export mode issues both generation requests before polling. `artifacts/edit-provenance-probe.py` creates synthetic edit files; `artifacts/summarize-provenance-probes.py` saves sanitized fixtures and a structured summary.

## Cleanup

After the final live action test passed, only the two synthetic probe projects were deleted using `DELETE /projects/{projectId}?option=DELETE_WITH_TM`. The API returned HTTP 200 with `{"success":true}` for both project `2857236` and project `2857304`. Their generated test translation-memory records were included in the requested deletion. Follow-up status requests returned HTTP 404 with `Unavailable data. Project not found.` for both IDs. Cleanup finished at 2026-09-08 09:32:51 UTC. The fixtures and report remain available; the live projects no longer exist.
