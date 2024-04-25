
class UserManager {
    isSignedIn() {
        return false;
    }
}

let userManager: UserManager | undefined = undefined;

export function getUserManager() {
    if (!userManager) {
        userManager = new UserManager();
    }
    return userManager;
}
