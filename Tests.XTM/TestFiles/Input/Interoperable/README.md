# Interoperable roundtrip fixtures

- `Inputs/cases.json`: project settings, upload options, and translations to submit for each case.
- `Inputs/source.*`: source files. The HTML fixture deliberately includes a UTF-8 BOM.
- `ExpectedOutputs/*.json`: expected upload response, prepared and uploaded source content, analyzed segments, generated-file status, translated XLIFF, and reconstructed native content.

Expected XLIFF snapshots retain source/target text, original XLIFF unit IDs, translation locks, and Blackbird exclusion markers. Generated unit IDs for HTML/text, project/job IDs, run-specific filename prefixes, XML formatting, and server-added metadata are excluded from comparisons. Units and analyzed sources are sorted by source text; segment order within each unit is preserved. Missing target elements and empty targets both represent untranslated text.

`Inputs/.runs/` holds temporary translated submissions with server-generated segment IDs during a live run; cleanup removes only that run's directory. Static fixtures are never rewritten by tests. API downloads use the test file manager's `TestFiles/Output` directory and are removed after the run.

The live test creates an isolated project using the configured credentials and fixture customer/workflow IDs, then deletes that project and its test TM. Source locales must match the fixture project. Review fixture changes as expected behavior changes; do not regenerate expected outputs from actual API results automatically.
