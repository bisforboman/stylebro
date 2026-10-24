# Trial 52 host soak on the new host code

After the re-check, #298 (faster re-checks) and #299 (merged fix rounds) merged into main (17:34 and 18:01 UTC), which
restarted the soak count. So one more round ran on a LOCAL merge of main cb47c2d (with both PRs) into this branch at
fa109a5 (merge commit 66fceb4, not pushed; no conflicts), packed as 0.8.0-trial52.3, both engines, fresh clones, the full
steps (preview, setup, format, check, check --build, TWAE Debug/Release, tests).

Result: on all four repositories the engines agree: trees (git diff, status), exit codes (all 0; error-or's tests 1 for
net462/Mono as untouched), check and check --build --json, preview/format/check/check --build logs (timings and paths
aside; error-or's check lists its three framework runs in another order). Trees equal the 52.2 re-check's but for the
package version string. Tests as untouched (error-or 228 x 2 frameworks, DbUp 100, figgle 323 + 30 + 4, EasyNetQ
443 + 14 + 3; Hosepipe's untouched-flaky set aside).

Format (s, default/host, error-or, DbUp, figgle, EasyNetQ): 68s/27s 49s/36s 90s/50s 148s/61s.
