[ ] Pagination for codes

[v] Allow the app to be navigated without any Enter key presses

[x] Replace Thread.Sleep with tasks?

[ ] Review code for security

[ ] Review code for other issues

[ ] Refactor code for better readability

[ ] Add mode options for when adding codes, e.g. different expiry times etc




**Helper code**

Generate mock 52-long strings of alpha-digit chars, in JS - for testing some things in the app
```code = (max) => {
    let res = '';
    const choices = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let i = 0;
    while (i++ < max)
        res += choices[Math.round(Math.random() * choices.length)];
    return res
}
```